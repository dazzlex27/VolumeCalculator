using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Timers;
using DeviceIntegration.Cameras;
using DeviceIntegration.RangeMeters;
using FrameProcessor;
using FrameProviders;
using Primitives;
using Primitives.Logging;

namespace VCServer
{
	public class VolumeCalculationLogic
	{
		public event Action<VolumeCalculatorResultData> CalculationFinished;

		private readonly ILogger _logger;
		private readonly DepthMapProcessor _processor;
		private readonly IFrameProvider _frameProvider;
		private readonly IRangeMeter _rangeMeter;
		private readonly IIpCamera _ipCamera;
		
		private readonly int _requiredSampleCount;
		private readonly string _barcode;
		private readonly int _calculationIndex;
		private readonly short _floorDepth;
		private readonly short _cutOffDepth;
		private readonly bool _dm1AlgorithmEnabled;
		private readonly bool _dm2AlgorithmEnabled;
		private readonly bool _rgbAlgorithmEnabled;
		private readonly string _photoDirectoryPath;
		private readonly int _rangeMeterCorrectionValueMm;

		private readonly Timer _updateTimeoutTimer;
		private readonly List<ObjectVolumeData> _results;

		private short _calculatedDistance;
		private bool _useColorData;

		private bool _colorFrameReady;
		private bool _depthFrameReady;

		private ImageData _latestColorFrame;
		private DepthMap _latestDepthMap;

		private int _samplesLeft;
		private AlgorithmSelectionStatus _selectedAlgorithm;
		private bool _wasRangeMeterUsed;

		private bool HasCompletedFirstRun => _samplesLeft != _requiredSampleCount;

		public VolumeCalculationLogic(ILogger logger, DepthMapProcessor processor, IFrameProvider frameProvider, 
			IRangeMeter rangeMeter, IIpCamera ipCamera, VolumeCalculationData calculationData)
		{
			_logger = logger;
			_processor = processor;
			_frameProvider = frameProvider;
			_rangeMeter = rangeMeter;
			_ipCamera = ipCamera;
			
			_requiredSampleCount = calculationData.RequiredSampleCount;
			_barcode = calculationData.Barcode;
			_calculationIndex = calculationData.CalculationIndex;
			_dm1AlgorithmEnabled = calculationData.Dm1AlgorithmEnabled;
			_dm2AlgorithmEnabled = calculationData.Dm2AlgorithmEnabled;
			_rgbAlgorithmEnabled = calculationData.RgbAlgorithmEnabled;
			_photoDirectoryPath = calculationData.PhotosDirectoryPath;
			_floorDepth = calculationData.FloorDepth;
			_cutOffDepth = calculationData.CutOffDepth;
			_rangeMeterCorrectionValueMm = calculationData.RangeMeterCorrectionValue;

			_samplesLeft = _requiredSampleCount;

			_useColorData = true;

			_results = new List<ObjectVolumeData>();

			frameProvider.UnrestrictedDepthFrameReady += OnDepthFrameReady;
			frameProvider.UnrestrictedColorFrameReady += OnColorFrameReady;

			_updateTimeoutTimer = new Timer(3000) {AutoReset = false};
			_updateTimeoutTimer.Elapsed += OnTimerElapsed;
			_updateTimeoutTimer.Start();
		}

		public void Abort()
		{
			AbortInternal(CalculationStatus.AbortedByUser);
		}

		private void CleanUp()
		{
			_updateTimeoutTimer.Stop();
			_frameProvider.UnrestrictedColorFrameReady -= OnColorFrameReady;
			_frameProvider.UnrestrictedDepthFrameReady -= OnDepthFrameReady;
		}

		private void AbortInternal(CalculationStatus status)
		{
			CleanUp();
			var result = new ObjectVolumeData(0, 0, 0);
			var resultData = new VolumeCalculatorResultData(result, status, _latestColorFrame,
					_selectedAlgorithm, false);
			CalculationFinished?.Invoke(resultData);
		}

		private void AdvanceCalculation(DepthMap depthMap, ImageData image)
		{
			_updateTimeoutTimer.Stop();

			if (!HasCompletedFirstRun)
			{
				if (_rangeMeter != null)
				{
					var reading = _rangeMeter.GetReading() + _rangeMeterCorrectionValueMm;
					var readingIsInRange = reading > short.MinValue && reading < short.MaxValue;
					_calculatedDistance = readingIsInRange ? (short)reading : (short)0;
					_logger.LogInfo($"Range meter reading={reading}, floorDepth={_floorDepth}");
					if (_floorDepth - _calculatedDistance < 0)
					{
						_logger.LogError("range meter reading was below floor depth");
						_calculatedDistance = 0;
					}

					if (_calculatedDistance <= 0)
						_logger.LogError("Failed to get proper range meter reading, will use depth calculation");
				}
				else
					_logger.LogInfo($"Range meter is not enabled - will use depth calculation");

				var debugFileName = $"{_barcode}_{_calculationIndex}";

				SelectAlgorithmAndAbortIfFailed(debugFileName);
				SaveDebugData(debugFileName);
			}

			var currentResult = _processor.CalculateVolume(depthMap, image, _calculatedDistance, _selectedAlgorithm);

			_results.Add(currentResult);
			_samplesLeft--;

			if (_samplesLeft > 0)
			{
				_colorFrameReady = false;
				_depthFrameReady = false;
				_updateTimeoutTimer.Start();

				return;
			}

			CleanUp();
			
			var totalResult = AggregateCalculationsData();
			var resultStatus = totalResult != null ? CalculationStatus.Successful : CalculationStatus.Error;

			var resultData =
				new VolumeCalculatorResultData(totalResult, resultStatus, _latestColorFrame, _selectedAlgorithm,
					_wasRangeMeterUsed);
			
			CalculationFinished?.Invoke(resultData);
		}

		private void SelectAlgorithmAndAbortIfFailed(string debugFileName)
		{
			var algorithmSelectionData = new AlgorithmSelectionData(_latestDepthMap, _latestColorFrame, _calculatedDistance,
				_dm1AlgorithmEnabled, _dm2AlgorithmEnabled, _rgbAlgorithmEnabled, debugFileName);
			
			var algorithmSelectionResult =_processor.SelectAlgorithm(algorithmSelectionData);
			_selectedAlgorithm = algorithmSelectionResult.Status;
			_wasRangeMeterUsed = algorithmSelectionResult.RangeMeterWasUsed;

			switch (_selectedAlgorithm)
			{
				case AlgorithmSelectionStatus.DataIsInvalid:
					_logger.LogError("Failed to select algorithm: data was invalid");
					break;
				case AlgorithmSelectionStatus.NoAlgorithmsAllowed:
					_logger.LogError("Failed to select algorithm: no modes were available");
					break;
				case AlgorithmSelectionStatus.NoObjectFound:
					_logger.LogError("Failed to select algorithm: no objects were found");
					break;
				case AlgorithmSelectionStatus.Dm1:
					_useColorData = false;
					_logger.LogInfo("Selected algorithm: dm1");
					break;
				case AlgorithmSelectionStatus.Dm2:
					_useColorData = false;
					_logger.LogInfo("Selected algorithm: dm2");
					break;
				case AlgorithmSelectionStatus.Rgb:
					_useColorData = true;
					_logger.LogInfo("Selected algorithm: rgb");
					break;
			}

			if (_selectedAlgorithm == AlgorithmSelectionStatus.NoObjectFound)
			{
				AbortInternal(CalculationStatus.ObjectNotFound);
				return;
			}

			if (_selectedAlgorithm < 0)
				AbortInternal(CalculationStatus.FailedToSelectAlgorithm);
		}

		private void OnColorFrameReady(ImageData image)
		{
			_latestColorFrame = image;

			_colorFrameReady = true;

			if (!_depthFrameReady)
				return;

			AdvanceCalculation(_latestDepthMap, _latestColorFrame);

			if (!_useColorData)
				_frameProvider.UnrestrictedColorFrameReady -= OnColorFrameReady;
		}

		private void OnDepthFrameReady(DepthMap depthMap)
		{
			_latestDepthMap = depthMap;

			_depthFrameReady = true;

			if (_useColorData && !_colorFrameReady || !HasCompletedFirstRun)
				return;

			AdvanceCalculation(_latestDepthMap, _latestColorFrame);
		}

		private ObjectVolumeData AggregateCalculationsData()
		{
			try
			{
				var lengths = _results.Where(r => r != null).Select(r => r.LengthMm).ToArray();
				var widths = _results.Where(r => r != null).Select(r => r.WidthMm).ToArray();
				var heights = _results.Where(r => r != null).Select(r => r.HeightMm).ToArray();

				var joinedLengths = string.Join(",", lengths);
				var joinedWidths = string.Join(",", widths);
				var joinedHeights= string.Join(",", heights);
				_logger.LogInfo($"Measured values: {{{joinedLengths}}}; {{{joinedWidths}}}; {{{joinedHeights}}}");

				var modeLength = lengths.GroupBy(x => x).OrderByDescending(g => g.Count()).First().Key;
				var modeWidth = widths.GroupBy(x => x).OrderByDescending(g => g.Count()).First().Key;
				var modeHeight = heights.GroupBy(x => x).OrderByDescending(g => g.Count()).First().Key;

				return new ObjectVolumeData(modeLength, modeWidth, modeHeight);
			}
			catch (Exception ex)
			{
				_logger.LogException("Failed to aggregate calculation results", ex);
				return null;
			}
		}

		private void OnTimerElapsed(object sender, ElapsedEventArgs e)
		{
			_logger.LogInfo($"Timeout timer elapsed (samplesLeft={_samplesLeft}), aborting calculation...");

			CleanUp();
			AbortInternal(CalculationStatus.TimedOut);
		}

		private void SaveDebugData(string debugFileName)
		{
			try
			{
				var baseFilePath = Path.Combine(_photoDirectoryPath, debugFileName);
				_logger.LogInfo($"Calculation Base filename = {debugFileName}");

				if (_latestColorFrame != null)
				{
					var colorFileName = $"{baseFilePath}_color.png";
					ImageUtils.SaveImageDataToFile(_latestColorFrame, colorFileName);
				}

				if (_latestDepthMap != null)
				{
					var depthFileName = $"{baseFilePath}_depth.png";
					var depthCameraParams = _frameProvider.GetDepthCameraParams();

					DepthMapUtils.SaveDepthMapImageToFile(_latestDepthMap, depthFileName,
						depthCameraParams.MinDepth, depthCameraParams.MaxDepth, _cutOffDepth);
				}
				
				var cameraIsEnabled = _ipCamera != null && _ipCamera.Initialized();
				if (!cameraIsEnabled)
					return;
				
				try
				{
					var cameraFileName = $"{baseFilePath}_camera.png";
					var ipCameraFrame = Task.Run(() => _ipCamera.GetSnaphostAsync()).Result;
					ImageUtils.SaveImageDataToFile(ipCameraFrame, cameraFileName);
				}
				catch (Exception ex)
				{
					_logger.LogException("Failed to get IP camera frame", ex);
				}
			}
			catch (Exception ex)
			{
				_logger.LogException("Failed to save debug data", ex);
			}
		}
	}
}