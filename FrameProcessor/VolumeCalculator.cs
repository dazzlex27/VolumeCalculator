using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Timers;
using FrameProviders;
using Primitives;
using Primitives.Logging;

namespace FrameProcessor
{
	public class VolumeCalculator : IDisposable
	{
		public event Action<ObjectVolumeData, CalculationStatus> CalculationFinished;

		private readonly ILogger _logger;
		private readonly FrameProvider _frameProvider;
		private readonly DepthMapProcessor _processor;
		private readonly ApplicationSettings _settings;
		private readonly bool _usingColorData;

		private readonly Timer _timer;
		private readonly List<ObjectVolumeData> _results;

		private bool _colorFrameReady;
		private bool _depthFrameReady;

		private ImageData _latestColorFrame;
		private DepthMap _latestDepthMap;

		private int _samplesLeft;
		private bool _hasCompletedFirstRun;

		private WorkingAreaMask _depthMapMask;

		public bool IsRunning { get; private set; }

		public VolumeCalculator(ILogger logger, FrameProvider frameProvider, DepthMapProcessor processor, ApplicationSettings settings, 
			bool usingColorData)
		{
			_logger = logger;
			_frameProvider = frameProvider;
			_processor = processor;
			_settings = settings;
			_samplesLeft = settings.SampleDepthMapCount;
			_usingColorData = usingColorData;

			if (_frameProvider == null)
				throw new ArgumentNullException(nameof(_frameProvider));

			_results = new List<ObjectVolumeData>();

			_frameProvider.UnrestrictedDepthFrameReady += OnDepthFrameReady;
			_frameProvider.UnrestrictedColorFrameReady += OnColorFrameReady;

			_timer = new Timer(3000);
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

		private void CalculateDepthMapZoneMask(DepthMap depthMap)
		{
			if (_settings.UseDepthMask)
			{
				var points = _settings.DepthMaskContour.ToArray();
				var maskData = GeometryUtils.CreateWorkingAreaMask(points, depthMap.Width, depthMap.Height);
				_depthMapMask = new WorkingAreaMask(depthMap.Width, depthMap.Height, maskData);
			}
			else
			{
				var maskData = Enumerable.Repeat(true, depthMap.Width * depthMap.Height).ToArray();
				_depthMapMask = new WorkingAreaMask(depthMap.Width, depthMap.Height, maskData);
			}
		}

		private DepthMap GetMaskedDepthMap(DepthMap depthMap)
		{
			if (!_settings.UseDepthMask)
				return depthMap;

			var maskedMap = new DepthMap(depthMap);

			GeometryUtils.ApplyWorkingAreaMask(maskedMap, _depthMapMask.Data);

			return maskedMap;
		}

		private void AdvanceCalculation(DepthMap depthMap, ImageData image)
		{
			_timer.Stop();

			if (!_hasCompletedFirstRun)
			{
				CalculateDepthMapZoneMask(depthMap);
				SaveDebugData();
				_hasCompletedFirstRun = true;
			}

			var maskedMap = GetMaskedDepthMap(depthMap);

			var currentResult = _usingColorData
				? _processor.CalculateObjectVolumeAlt(maskedMap, image)
				: _processor.CalculateVolume(maskedMap);
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

			if (!_usingColorData)
				_frameProvider.UnrestrictedColorFrameReady -= OnColorFrameReady;
		}

		private void OnDepthFrameReady(DepthMap depthMap)
		{
			_latestDepthMap = depthMap;

			_depthFrameReady = true;

			if (_usingColorData && !_colorFrameReady || !_hasCompletedFirstRun)
				return;

			AdvanceCalculation(_latestDepthMap, _latestColorFrame);
			_colorFrameReady = false;
			_depthFrameReady = false;
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
					var colorFrameFileName = Path.Combine(_settings.PhotosDirectoryPath, $"{calculationIndex}_color.png");
					ImageUtils.SaveImageDataToFile(_latestColorFrame, colorFrameFileName);
				}

				if (_latestDepthMap == null)
					return;

				var depthCameraParams = _frameProvider.GetDepthCameraParams();

				var depthFrameFileName = Path.Combine(_settings.PhotosDirectoryPath, $"{calculationIndex}_depth.png");

				var cutOffDepth = (short) (_settings.FloorDepth - _settings.MinObjectHeight);
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