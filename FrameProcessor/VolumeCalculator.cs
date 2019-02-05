using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Timers;
using FrameProviders;
using Primitives;
using Primitives.Logging;
using Primitives.Settings;

namespace FrameProcessor
{
	public class VolumeCalculator : IDisposable
	{
		public event Action<ObjectVolumeData, CalculationStatus> CalculationFinished;

		private readonly ILogger _logger;
		private readonly FrameProvider _frameProvider;
		private readonly DepthMapProcessor _processor;
		private readonly ApplicationSettings _settings;

		private bool _applyPerspective;
		private bool _useColorData;

		private readonly Timer _timer;
		private readonly List<ObjectVolumeData> _results;

		private bool _colorFrameReady;
		private bool _depthFrameReady;

		private ImageData _latestColorFrame;
		private DepthMap _latestDepthMap;

		private int _samplesLeft;

		private AlgorithmSelectionResult _selectedAlgorithm;

		private readonly bool _maskMode;

		public bool IsRunning { get; private set; }

		private bool HasCompletedFirstRun => _samplesLeft != _settings.AlgorithmSettings.SampleDepthMapCount;

		public VolumeCalculator(ILogger logger, FrameProvider frameProvider, DepthMapProcessor processor, ApplicationSettings settings, 
			bool maskMode)
		{
			_logger = logger;
			_frameProvider = frameProvider ?? throw new ArgumentNullException(nameof(frameProvider));
			_processor = processor ?? throw new ArgumentException(nameof(processor));
			_settings = settings ?? throw new ArgumentException(nameof(settings));
			_samplesLeft = settings.AlgorithmSettings.SampleDepthMapCount;

			_maskMode = maskMode;

			_applyPerspective = false;
			_useColorData = true;

			_results = new List<ObjectVolumeData>();

			_frameProvider.UnrestrictedDepthFrameReady += OnDepthFrameReady;
			_frameProvider.UnrestrictedColorFrameReady += OnColorFrameReady;

			_timer = new Timer(3000) {AutoReset = false};
			_timer.Elapsed += OnTimerElapsed;
			_timer.Start();

			IsRunning = true;
		}

		public void Dispose()
		{
			_timer.Stop();
			IsRunning = false;
			_frameProvider.UnrestrictedColorFrameReady -= OnColorFrameReady;
			_frameProvider.UnrestrictedDepthFrameReady -= OnDepthFrameReady;
		}

		public void Abort()
		{
			AbortInternal(CalculationStatus.AbortedByUser);
		}

		private void AbortInternal(CalculationStatus status)
		{
			Dispose();
			CalculationFinished?.Invoke(null, status);
		}

		private void AdvanceCalculation(DepthMap depthMap, ImageData image)
		{
			_timer.Stop();

			if (!HasCompletedFirstRun)
			{
				SelectAlgorithm();
				SaveDebugData();
			}

			var currentResult = CalculateVolume(depthMap, image, _applyPerspective, !HasCompletedFirstRun);

			_results.Add(currentResult);
			_samplesLeft--;

			if (_samplesLeft > 0)
			{
				_timer.Start();
				return;
			}

			IsRunning = false;
			var totalResult = AggregateCalculationsData();

			if (totalResult != null)
				CalculationFinished?.Invoke(totalResult, CalculationStatus.Sucessful);
			else
				CalculationFinished?.Invoke(null, CalculationStatus.Error);
		}

		private void SelectAlgorithm()
		{
			var dm1Enabled = _settings.AlgorithmSettings.EnableDmAlgorithm;
			var dm2Enabled = _settings.AlgorithmSettings.EnablePerspectiveDmAlgorithm;
			var rgbEnabled = _settings.AlgorithmSettings.EnableRgbAlgorithm;

			_selectedAlgorithm = _processor.SelectAlgorithm(_latestDepthMap, _latestColorFrame, 
				dm1Enabled, dm2Enabled, rgbEnabled);

			switch (_selectedAlgorithm)
			{
				case AlgorithmSelectionResult.DataIsInvalid:
					_logger.LogError("Failed to select algorithm: data was invalid");
					break;
				case AlgorithmSelectionResult.NoModesAreAvailable:
					_logger.LogError("Failed to select algorithm: no modes were available");
					break;
				case AlgorithmSelectionResult.NoObjectFound:
					_logger.LogError("Failed to select algorithm: no objects were found");
					break;
				case AlgorithmSelectionResult.Dm:
					_useColorData = false;
					_applyPerspective = false;
					_logger.LogInfo("Selected algorithm: dm1");
					break;
				case AlgorithmSelectionResult.DmPersp:
					_useColorData = false;
					_applyPerspective = true;
					_logger.LogInfo("Selected algorithm: dm2");
					break;
				case AlgorithmSelectionResult.Rgb:
					_useColorData = true;
					_applyPerspective = false;
					_logger.LogInfo("Selected algorithm: rgb");
					break;
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
			_colorFrameReady = false;
			_depthFrameReady = false;

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
			_colorFrameReady = false;
			_depthFrameReady = false;
		}

		private ObjectVolumeData CalculateVolume(DepthMap depthMap, ImageData image, bool applyPerspective, bool needToSavePics)
		{
			return _useColorData
				? _processor.CalculateObjectVolumeRgb(depthMap, image, applyPerspective, needToSavePics, _maskMode)
				: _processor.CalculateVolumeDepth(depthMap, applyPerspective, needToSavePics, _maskMode);
		}

		private ObjectVolumeData AggregateCalculationsData()
		{
			try
			{
				var lengths = _results.Select(r => r.Length).ToArray();
				var widths = _results.Select(r => r.Width).ToArray();
				var heights = _results.Select(r => r.Height).ToArray();

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
			CalculationFinished?.Invoke(null, CalculationStatus.TimedOut);
		}

		private void SaveDebugData()
		{
			try
			{
				Directory.CreateDirectory(Constants.DebugDataDirectoryName);
				var calculationIndex = IoUtils.GetCurrentUniversalObjectCounter();

				if (_latestColorFrame != null)
				{
					var colorFrameFileName = Path.Combine(_settings.IoSettings.PhotosDirectoryPath, $"{calculationIndex}_color.png");
					ImageUtils.SaveImageDataToFile(_latestColorFrame, colorFrameFileName);
				}

				if (_latestDepthMap == null)
					return;

				var depthCameraParams = _frameProvider.GetDepthCameraParams();

				var depthFrameFileName = Path.Combine(_settings.IoSettings.PhotosDirectoryPath, $"{calculationIndex}_depth.png");

				var cutOffDepth = (short) (_settings.AlgorithmSettings.FloorDepth - _settings.AlgorithmSettings.MinObjectHeight);
				DepthMapUtils.SaveDepthMapImageToFile(_latestDepthMap, depthFrameFileName,
					depthCameraParams.MinDepth, depthCameraParams.MaxDepth, cutOffDepth);
			}
			catch (Exception ex)
			{
				_logger.LogException("Failed to save debug data", ex);
			}
		}
	}
}