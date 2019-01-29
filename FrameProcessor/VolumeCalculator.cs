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

		private readonly bool _applyPerspective;
		private readonly bool _useColorData;

		private readonly Timer _timer;
		private readonly List<ObjectVolumeData> _results;

		private bool _colorFrameReady;
		private bool _depthFrameReady;

		private ImageData _latestColorFrame;
		private DepthMap _latestDepthMap;

		private int _samplesLeft;

		public bool IsRunning { get; private set; }


		private bool HasCompletedFirstRun => _samplesLeft != _settings.AlgorithmSettings.SampleDepthMapCount;

		public VolumeCalculator(ILogger logger, FrameProvider frameProvider, DepthMapProcessor processor, ApplicationSettings settings, 
			bool applyPerspective, bool useColorData)
		{
			_logger = logger;
			_frameProvider = frameProvider ?? throw new ArgumentNullException(nameof(frameProvider));
			_processor = processor ?? throw new ArgumentException(nameof(processor));
			_settings = settings ?? throw new ArgumentException(nameof(settings));
			_samplesLeft = settings.AlgorithmSettings.SampleDepthMapCount;

			_applyPerspective = applyPerspective;
			_useColorData = useColorData;

			logger.LogInfo($"! creating volume calculator sampleCount={settings.AlgorithmSettings.SampleDepthMapCount}");

			_results = new List<ObjectVolumeData>();

			_frameProvider.UnrestrictedDepthFrameReady += OnDepthFrameReady;
			_frameProvider.UnrestrictedColorFrameReady += OnColorFrameReady;

			_timer = new Timer(3000) {AutoReset = false};
			_timer.Elapsed += Timer_Elapsed;
			_timer.Start();

			IsRunning = true;
		}

		public void Dispose()
		{
			IsRunning = false;
			_frameProvider.UnrestrictedColorFrameReady -= OnColorFrameReady;
			_frameProvider.UnrestrictedDepthFrameReady -= OnDepthFrameReady;
		}

		public void Abort()
		{
			Dispose();
			CalculationFinished?.Invoke(null, CalculationStatus.Aborted);
		}

		private void AdvanceCalculation(DepthMap depthMap, ImageData image)
		{
			_timer.Stop();

			if (!HasCompletedFirstRun)
				SaveDebugData();

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
				? _processor.CalculateObjectVolumeRgb(depthMap, image, applyPerspective, needToSavePics)
				: _processor.CalculateVolumeDepth(depthMap, applyPerspective, needToSavePics);
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

		private void Timer_Elapsed(object sender, ElapsedEventArgs e)
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