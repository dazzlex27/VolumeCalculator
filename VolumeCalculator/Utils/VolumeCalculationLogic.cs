using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Timers;
using FrameProcessor;
using FrameProcessor.Native;
using Primitives;
using Primitives.Logging;

namespace VolumeCalculator.Utils
{
	internal class VolumeCalculationLogic
	{
		public event Action<ObjectVolumeData, CalculationStatus, ImageData> CalculationFinished;

		private readonly ILogger _logger;
		private readonly DeviceSet _deviceSet;
		private readonly DepthMapProcessor _processor;
		private readonly int _requiredSampleCount;
		private readonly string _barcode;
		private readonly int _calculationIndex;
		private readonly short _cutOffDepth;
		private readonly bool _dm1AlgorithmEnabled;
		private readonly bool _dm2AlgorithmEnabled;
		private readonly bool _rgbAlgorithmEnabled;
		private readonly string _photoDirectoryPath;

		private readonly Timer _UpdateTimeoutTimer;
		private readonly List<ObjectVolumeData> _results;

		private short _calculatedDistance;
		private bool _useColorData;

		private bool _colorFrameReady;
		private bool _depthFrameReady;

		private ImageData _latestColorFrame;
		private DepthMap _latestDepthMap;

		private int _samplesLeft;

		private AlgorithmSelectionResult _selectedAlgorithm;

		public bool IsRunning { get; private set; }

		private bool HasCompletedFirstRun => _samplesLeft != _requiredSampleCount;

		public VolumeCalculationLogic(ILogger logger, VolumeCalculationData calculationData)
		{
			_logger = logger;
			_deviceSet = calculationData.DeviceSet;
			_processor = calculationData.DepthMapProcessor;
			_requiredSampleCount = calculationData.RequiredSampleCount;
			_barcode = calculationData.Barcode;
			_calculationIndex = calculationData.CalculationIndex;
			_dm1AlgorithmEnabled = calculationData.Dm1AlgorithmEnabled;
			_dm2AlgorithmEnabled = calculationData.Dm2AlgorithmEnabled;
			_rgbAlgorithmEnabled = calculationData.RgbAlgorithmEnabled;
			_photoDirectoryPath = calculationData.PhotosDirectoryPath;
			_cutOffDepth = calculationData.CutOffDepth;

			_samplesLeft = _requiredSampleCount;

			_useColorData = true;

			_results = new List<ObjectVolumeData>();

			_deviceSet.FrameProvider.UnrestrictedDepthFrameReady += OnDepthFrameReady;
			_deviceSet.FrameProvider.UnrestrictedColorFrameReady += OnColorFrameReady;

			_UpdateTimeoutTimer = new Timer(3000) {AutoReset = false};
			_UpdateTimeoutTimer.Elapsed += OnTimerElapsed;
			_UpdateTimeoutTimer.Start();

			IsRunning = true;
		}

		public void Abort()
		{
			AbortInternal(CalculationStatus.AbortedByUser);
		}

		private void CleanUp()
		{
			_UpdateTimeoutTimer.Stop();
			_deviceSet.FrameProvider.UnrestrictedColorFrameReady -= OnColorFrameReady;
			_deviceSet.FrameProvider.UnrestrictedDepthFrameReady -= OnDepthFrameReady;
			IsRunning = false;
		}

		private void AbortInternal(CalculationStatus status)
		{
			CleanUp();
			CalculationFinished?.Invoke(new ObjectVolumeData(0, 0, 0), status, _latestColorFrame);
		}

		private void AdvanceCalculation(DepthMap depthMap, ImageData image)
		{
			_UpdateTimeoutTimer.Stop();

			if (!HasCompletedFirstRun)
			{
				if (_deviceSet?.RangeMeter != null)
				{
					var reading = _deviceSet.RangeMeter.GetReading();
					var readingIsInRange = reading > short.MinValue && reading < short.MaxValue;
					_calculatedDistance = readingIsInRange ? (short)reading : (short)0;
					_logger.LogInfo($"Range meter reading - {reading}");
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
				_UpdateTimeoutTimer.Start();

				return;
			}

			CleanUp();

			var totalResult = AggregateCalculationsData();
			if (totalResult != null)
				CalculationFinished?.Invoke(totalResult, CalculationStatus.Sucessful, _latestColorFrame);
			else
				CalculationFinished?.Invoke(null, CalculationStatus.Error, _latestColorFrame);
		}

		private void SelectAlgorithmAndAbortIfFailed(string debugFileName)
		{
			_selectedAlgorithm = _processor.SelectAlgorithm(_latestDepthMap, _latestColorFrame, _calculatedDistance,
				_dm1AlgorithmEnabled, _dm2AlgorithmEnabled, _rgbAlgorithmEnabled, debugFileName);

			switch (_selectedAlgorithm)
			{
				case AlgorithmSelectionResult.DataIsInvalid:
					_logger.LogError("Failed to select algorithm: data was invalid");
					break;
				case AlgorithmSelectionResult.NoAlgorithmsAllowed:
					_logger.LogError("Failed to select algorithm: no modes were available");
					break;
				case AlgorithmSelectionResult.NoObjectFound:
					_logger.LogError("Failed to select algorithm: no objects were found");
					break;
				case AlgorithmSelectionResult.Dm1:
					_useColorData = false;
					_logger.LogInfo("Selected algorithm: dm1");
					break;
				case AlgorithmSelectionResult.Dm2:
					_useColorData = false;
					_logger.LogInfo("Selected algorithm: dm2");
					break;
				case AlgorithmSelectionResult.Rgb:
					_useColorData = true;
					_logger.LogInfo("Selected algorithm: rgb");
					break;
			}

			if (_selectedAlgorithm == AlgorithmSelectionResult.NoObjectFound)
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
				_deviceSet.FrameProvider.UnrestrictedColorFrameReady -= OnColorFrameReady;
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
				var lengths = _results.Select(r => r.LengthMm).ToArray();
				var widths = _results.Select(r => r.WidthMm).ToArray();
				var heights = _results.Select(r => r.HeightMm).ToArray();

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
					var depthCameraParams = _deviceSet.FrameProvider.GetDepthCameraParams();

					DepthMapUtils.SaveDepthMapImageToFile(_latestDepthMap, depthFileName,
						depthCameraParams.MinDepth, depthCameraParams.MaxDepth, _cutOffDepth);
				}

				if (_deviceSet.IpCamera != null && _deviceSet.IpCamera.Initialized())
				{
					try
					{
						var cameraFileName = $"{baseFilePath}_camera.png";
						var ipCameraFrame = Task.Run(() => _deviceSet.IpCamera.GetSnaphostAsync()).Result;
						ImageUtils.SaveImageDataToFile(ipCameraFrame, cameraFileName);
					}
					catch (Exception ex)
					{
						_logger.LogException("Failed to get IP camera frame", ex);
					}
				}
			}
			catch (Exception ex)
			{
				_logger.LogException("Failed to save debug data", ex);
			}
		}
	}
}