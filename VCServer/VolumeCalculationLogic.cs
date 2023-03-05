using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;
using DeviceIntegration.Cameras;
using DeviceIntegration.RangeMeters;
using FrameProcessor;
using DeviceIntegration.FrameProviders;
using Primitives;
using Primitives.Logging;
using ProcessingUtils;
using Timer = System.Timers.Timer;

namespace VCServer
{
	public class VolumeCalculationLogic
	{
		public event Action<VolumeCalculatorResultData> CalculationFinished;

		private readonly ILogger _logger;
		private readonly IFrameProvider _frameProvider;
		private readonly IRangeMeter _rangeMeter;
		private readonly IIpCamera _ipCamera;
		
		private readonly VolumeCalculationData _calculationData;
		private readonly int _requiredSampleCount;
		private readonly string _barcode;
		private readonly int _calculationIndex;
		private readonly short _floorDepth;
		private readonly short _cutOffDepth;
		private readonly string _photoDirectoryPath;
		private readonly int _rangeMeterCorrectionValueMm;

		private readonly Timer _updateTimeoutTimer;

		private ImageData _latestColorFrame;
		private DepthMap _latestDepthMap;

		private readonly List<ImageData> _images;
		private readonly List<DepthMap> _depthMaps;
		
		private readonly VolumeCalculator _calculator;

		private readonly CancellationToken _token;

		public VolumeCalculationLogic(ILogger logger, DepthMapProcessor processor, IFrameProvider frameProvider, 
			IRangeMeter rangeMeter, IIpCamera ipCamera, VolumeCalculationData calculationData)
		{
			_logger = logger;
			_frameProvider = frameProvider;
			_rangeMeter = rangeMeter;
			_ipCamera = ipCamera;

			_calculationData = calculationData;
			_requiredSampleCount = calculationData.RequiredSampleCount;
			_barcode = calculationData.Barcode;
			_calculationIndex = calculationData.CalculationIndex;
			_photoDirectoryPath = calculationData.PhotosDirectoryPath;
			_floorDepth = calculationData.FloorDepth;
			_cutOffDepth = calculationData.CutOffDepth;
			_rangeMeterCorrectionValueMm = calculationData.RangeMeterCorrectionValue;

			_images = new List<ImageData>();
			_depthMaps = new List<DepthMap>();

			_calculator = new VolumeCalculator(logger, processor);

			frameProvider.UnrestrictedDepthFrameReady += OnDepthFrameReady;
			frameProvider.UnrestrictedColorFrameReady += OnColorFrameReady;

			_token = new CancellationToken();

			_updateTimeoutTimer = new Timer(5000) {AutoReset = false};
			_updateTimeoutTimer.Elapsed += OnTimerElapsed;
			_updateTimeoutTimer.Start();
		}

		public void Dispose()
		{
			AbortInternal(CalculationStatus.AbortedByUser);
		}

		private void CleanUp()
		{
			_token.ThrowIfCancellationRequested();
			_updateTimeoutTimer?.Stop();
			if (_frameProvider == null)
				return;
			
			_frameProvider.UnrestrictedColorFrameReady -= OnColorFrameReady;
			_frameProvider.UnrestrictedDepthFrameReady -= OnDepthFrameReady;
		}

		private void AbortInternal(CalculationStatus status)
		{
			CleanUp();
			var result = new ObjectVolumeData(0, 0, 0);
			var resultData = new VolumeCalculatorResultData(result, status, _latestColorFrame,
					AlgorithmSelectionStatus.Undefined, false);
			CalculationFinished?.Invoke(resultData);
		}

		private async Task PerformCalculation()
		{
			await SaveDebugDataAsync($"{_barcode}_{_calculationIndex}");
			var calculatedDistance = GetCalculatedDistance();
			
			var result = _calculator.Calculate(_images, _depthMaps, _calculationData, calculatedDistance);
			CalculationFinished?.Invoke(result);
			_updateTimeoutTimer.Stop();
		}

		private short GetCalculatedDistance()
		{
			short calculatedDistance = 0;
			
			if (_rangeMeter != null)
			{
				var reading = _rangeMeter.GetReading() + _rangeMeterCorrectionValueMm;
				var readingIsInRange = reading > short.MinValue && reading < short.MaxValue;
				calculatedDistance = readingIsInRange ? (short) reading : (short) 0;
				_logger.LogInfo($"Range meter reading={reading}, floorDepth={_floorDepth}");
				if (_floorDepth - calculatedDistance < 0)
				{
					_logger.LogError("range meter reading was below floor depth");
					calculatedDistance = 0;
				}

				if (calculatedDistance <= 0)
					_logger.LogError("Failed to get proper range meter reading, will use depth calculation");
			}
			else
				_logger.LogInfo($"Range meter is not enabled - will use depth calculation");

			return calculatedDistance;
		}
		
		private void OnColorFrameReady(ImageData image)
		{
			if (_images.Count == _requiredSampleCount)
				return;
			
			_logger.LogInfo($"added color frame, count={_images.Count}");
			
			_latestColorFrame = image;
			_images.Add(image);
			
			if (_depthMaps.Count == _requiredSampleCount)
				PerformCalculation();
		}

		private void OnDepthFrameReady(DepthMap depthMap)
		{
			if (_depthMaps.Count == _requiredSampleCount)
				return;
			
			_logger.LogInfo($"added depth frame, count={_depthMaps.Count}");
			
			_latestDepthMap = depthMap;
			_depthMaps.Add(depthMap);

			if (_images.Count == _requiredSampleCount)
				PerformCalculation();
		}

		private void OnTimerElapsed(object sender, ElapsedEventArgs e)
		{
			var samplesCollected = _depthMaps?.Count ?? 0;
			_logger.LogInfo($"Timeout timer elapsed (samples collected={samplesCollected}), aborting calculation...");

			CleanUp();
			AbortInternal(CalculationStatus.TimedOut);
		}

		private async Task SaveDebugDataAsync(string debugFileName)
		{
			try
			{
				var baseFilePath = Path.Combine(_photoDirectoryPath, debugFileName);
				_logger.LogInfo($"Calculation Base filename = {debugFileName}");

				if (_latestColorFrame != null)
				{
					var colorFileName = $"{baseFilePath}_color.png";
					await ImageUtils.SaveImageDataToFileAsync(_latestColorFrame, colorFileName);
				}

				if (_latestDepthMap != null)
				{
					var depthFileName = $"{baseFilePath}_depth.png";
					var depthCameraParams = _frameProvider.GetDepthCameraParams();

					await DepthMapUtils.SaveDepthMapImageToFile(_latestDepthMap, depthFileName,
						depthCameraParams.MinDepth, depthCameraParams.MaxDepth, _cutOffDepth);
				}
				
				var cameraIsEnabled = _ipCamera != null && _ipCamera.Initialized();
				if (!cameraIsEnabled)
					return;
				
				try
				{
					var cameraFileName = $"{baseFilePath}_camera.png";
					var ipCameraFrame = await _ipCamera.GetSnaphostAsync();
					await ImageUtils.SaveImageDataToFileAsync(ipCameraFrame, cameraFileName);
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
