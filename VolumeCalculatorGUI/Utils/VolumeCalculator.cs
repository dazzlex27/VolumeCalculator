﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Timers;
using FrameProcessor;
using Primitives;
using Primitives.Logging;
using Primitives.Settings;

namespace VolumeCalculatorGUI.Utils
{
	internal class VolumeCalculator
	{
		public event Action<ObjectVolumeData, CalculationStatus, ImageData> CalculationFinished;

		private readonly ILogger _logger;
		private readonly DeviceSet _deviceSet;
		private readonly DepthMapProcessor _processor;
		private readonly ApplicationSettings _settings;

		private readonly Timer _timer;
		private readonly List<ObjectVolumeData> _results;

		private long _measuredDistance;
		private bool _applyPerspective;
		private bool _useColorData;

		private bool _colorFrameReady;
		private bool _depthFrameReady;

		private ImageData _latestColorFrame;
		private DepthMap _latestDepthMap;

		private int _samplesLeft;

		private AlgorithmSelectionResult _selectedAlgorithm;

		private readonly bool _maskMode;

		public bool IsRunning { get; private set; }

		private bool HasCompletedFirstRun => _samplesLeft != _settings.AlgorithmSettings.SampleDepthMapCount;

		public VolumeCalculator(ILogger logger, DeviceSet deviceSet, DepthMapProcessor processor, ApplicationSettings settings, 
			bool maskMode)
		{
			_logger = logger;
			_deviceSet = deviceSet ?? throw new ArgumentException(nameof(deviceSet));
			_processor = processor ?? throw new ArgumentException(nameof(processor));
			_settings = settings ?? throw new ArgumentException(nameof(settings));
			_samplesLeft = settings.AlgorithmSettings.SampleDepthMapCount;

			_maskMode = maskMode;

			_applyPerspective = false;
			_useColorData = true;

			_results = new List<ObjectVolumeData>();

			_deviceSet.FrameProvider.UnrestrictedDepthFrameReady += OnDepthFrameReady;
			_deviceSet.FrameProvider.UnrestrictedColorFrameReady += OnColorFrameReady;

			_timer = new Timer(3000) {AutoReset = false};
			_timer.Elapsed += OnTimerElapsed;
			_timer.Start();

			IsRunning = true;
		}

		public void Abort()
		{
			AbortInternal(CalculationStatus.AbortedByUser);
		}

		private void CleanUp()
		{
			_timer.Stop();
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
			_timer.Stop();

			if (!HasCompletedFirstRun)
			{
				if (_deviceSet?.RangeMeter != null)
				{
					_measuredDistance = _deviceSet.RangeMeter.GetReading();
					_logger.LogInfo($"Measured distance - {_measuredDistance}");
					if (_measuredDistance <= 0)
						_logger.LogError("Failed to get range reading, will use depth calculation...");
				}

				SelectAlgorithm();
				SaveDebugData();
			}

			var currentResult = CalculateVolume(depthMap, image, _measuredDistance, _applyPerspective, !HasCompletedFirstRun);

			_results.Add(currentResult);
			_samplesLeft--;

			if (_samplesLeft > 0)
			{
				_colorFrameReady = false;
				_depthFrameReady = false;
				_timer.Start();

				return;
			}

			CleanUp();

			var totalResult = AggregateCalculationsData();
			if (totalResult != null)
				CalculationFinished?.Invoke(totalResult, CalculationStatus.Sucessful, _latestColorFrame);
			else
				CalculationFinished?.Invoke(null, CalculationStatus.Error, _latestColorFrame);
		}

		private void SelectAlgorithm()
		{
			var dm1Enabled = _settings.AlgorithmSettings.EnableDmAlgorithm;
			var dm2Enabled = _settings.AlgorithmSettings.EnablePerspectiveDmAlgorithm;
			var rgbEnabled = _settings.AlgorithmSettings.EnableRgbAlgorithm;

			_selectedAlgorithm = _processor.SelectAlgorithm(_latestDepthMap, _latestColorFrame, _measuredDistance,
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

		private ObjectVolumeData CalculateVolume(DepthMap depthMap, ImageData image, long measuredDistance, bool applyPerspective, 
			bool needToSavePics)
		{
			return _useColorData
				? _processor.CalculateObjectVolumeRgb(depthMap, image, measuredDistance, applyPerspective, needToSavePics, _maskMode)
				: _processor.CalculateVolumeDepth(depthMap, measuredDistance, applyPerspective, needToSavePics, _maskMode);
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

			CleanUp();
			AbortInternal(CalculationStatus.TimedOut);
		}

		private void SaveDebugData()
		{
			try
			{
				Directory.CreateDirectory(Constants.DebugDataDirectoryName);
				var calculationIndex = IoUtils.GetCurrentUniversalObjectCounter();
				_logger.LogInfo($"Global object ID = {calculationIndex}");

				if (_latestColorFrame != null)
				{
					var colorFrameFileName = Path.Combine(_settings.IoSettings.PhotosDirectoryPath, $"{calculationIndex}_color.png");
					ImageUtils.SaveImageDataToFile(_latestColorFrame, colorFrameFileName);
				}

				if (_latestDepthMap == null)
					return;

				var depthCameraParams = _deviceSet.FrameProvider.GetDepthCameraParams();

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