using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Common;
using FrameProviders;
using VolumeCalculatorGUI.Entities;
using VolumeCalculatorGUI.GUI.Utils;
using VolumeCalculatorGUI.Logic;
using VolumeCalculatorGUI.Utils;

namespace VolumeCalculatorGUI.GUI
{
	internal class SettingsWindowVm : BaseViewModel
	{
		private readonly DepthMapProcessor _volumeCalculator;
		private DepthMap _latestDepthMap;
		private readonly ILogger _logger;

		private WriteableBitmap _depthImageBitmap;
		private bool _useAreaMask;
		private MaskPolygonControlVm _maskPolygonControlVm;
		private short _maxDepth;
		private short _minDepth;
		private short _distanceToFloor;
		private short _minObjHeight;
		private byte _sampleCount;
		private string _outputPath;
		private bool _hasReceivedDepthMap;

		public WriteableBitmap DepthImageBitmap
		{
			get => _depthImageBitmap;
			set
			{
				if (Equals(_depthImageBitmap, value))
					return;

				_depthImageBitmap = value;
				OnPropertyChanged();
			}
		}

		public bool UseAreaMask
		{
			get => _useAreaMask;
			set
			{
				if (_useAreaMask == value)
					return;

				_useAreaMask = value;
				OnPropertyChanged();
			}
		}

		public MaskPolygonControlVm MaskPolygonControlVm
		{
			get => _maskPolygonControlVm;
			set
			{
				if (ReferenceEquals(_maskPolygonControlVm, value))
					return;

				_maskPolygonControlVm = value;
				OnPropertyChanged();
			}
		}

		public short MinDepth
		{
			get => _minDepth;
			set
			{
				if (_minDepth == value)
					return;

				_minDepth = value;
				OnPropertyChanged();
			}
		}

		public short MaxDepth
		{
			get => _maxDepth;
			set
			{
				if (_maxDepth == value)
					return;

				_maxDepth = value;
				OnPropertyChanged();
			}
		}

		public short FloorDepth
		{
			get => _distanceToFloor;
			set
			{
				if (_distanceToFloor == value)
					return;

				_distanceToFloor = value;
				OnPropertyChanged();
			}
		}

		public short MinObjHeight
		{
			get => _minObjHeight;
			set
			{
				if (_minObjHeight == value)
					return;

				_minObjHeight = value;
				OnPropertyChanged();
			}
		}

		public byte SampleCount
		{
			get => _sampleCount;
			set
			{
				if (value == _sampleCount)
					return;

				_sampleCount = value;
				OnPropertyChanged();
			}
		}

		public string OutputPath
		{
			get => _outputPath;
			set
			{
				if (_outputPath == value)
					return;

				_outputPath = value;
				OnPropertyChanged();
			}
		}

		public bool HasReceivedADepthMap
		{
			get => _hasReceivedDepthMap;
			set
			{
				if (_hasReceivedDepthMap == value)
					return;

				_hasReceivedDepthMap = value;
				OnPropertyChanged();
			}
		}

		public ICommand CalculateFloorDepthCommand { get; }

		public ICommand ResetSettingsCommand { get; }

		public SettingsWindowVm(ILogger logger, ApplicationSettings settings, DeviceParams deviceParams, 
			DepthMapProcessor volumeCalculator)
		{
			_logger = logger;
			_volumeCalculator = volumeCalculator;

			CalculateFloorDepthCommand = new CommandHandler(CalculateFloorDepth, true);
			ResetSettingsCommand = new CommandHandler(ResetSettings, true);

			var oldSettings = settings ?? ApplicationSettings.GetDefaultSettings();
			MinDepth = deviceParams.MinDepth;
			MaxDepth = deviceParams.MaxDepth;
			FillValuesFromSettings(oldSettings);
		}

		public ApplicationSettings GetSettings()
		{
			return new ApplicationSettings(FloorDepth, MinObjHeight, SampleCount, OutputPath, UseAreaMask, 
				new List<Point>(MaskPolygonControlVm.GetPolygonPoints()));
		}

		public void DepthFrameUpdated(DepthMap depthMap)
		{
			HasReceivedADepthMap = true;
			MaskPolygonControlVm.CanEditPolygon = true;
			_latestDepthMap = depthMap;

			var cutOffDepth = (short)(FloorDepth - MinObjHeight);
			var depthMapData = DepthMapUtils.GetColorizedDepthMapData(depthMap, MinDepth, FloorDepth,
				cutOffDepth);

			var imageWidth = depthMap.Width;
			var imageHeight = depthMap.Height;
			var fullRect = new Int32Rect(0, 0, imageWidth, imageHeight);

			var depthImageBitmap = new WriteableBitmap(imageWidth, imageHeight, 96, 96, PixelFormats.Rgb24, null);
			depthImageBitmap.WritePixels(fullRect, depthMapData, imageWidth * Constants.BytesPerPixel24, 0);
			depthImageBitmap.Freeze();

			DepthImageBitmap = depthImageBitmap;
		}

		private void CalculateFloorDepth()
		{
			try
			{
				if (_latestDepthMap == null)
				{
					MessageBox.Show("Нет кадров для обработки!", "Ошибка", MessageBoxButton.OK,
						MessageBoxImage.Exclamation);
					_logger.LogInfo("Attempted a volume check with no maps");

					return;
				}

				var floorDepth = _volumeCalculator.CalculateFloorDepth(_latestDepthMap);
				if (floorDepth <= 0)
					throw new ArgumentException("Floor depth calculation: return a value less than zero");

				FloorDepth = floorDepth;
				_logger.LogInfo($"Caculated floor depth as {floorDepth}mm");
			}
			catch (Exception ex)
			{
				_logger.LogException("Failed to calculate floor depth!", ex);

				MessageBox.Show("Во время вычисления произошла ошибка, автоматический расчёт не был выполнен", "Ошибка",
					MessageBoxButton.OK, MessageBoxImage.Error);
			}
		}

		private void ResetSettings()
		{
			if (MessageBox.Show("Сбросить настройки?", "Подтверждение", MessageBoxButton.YesNo,
				    MessageBoxImage.Question) != MessageBoxResult.Yes)
				return;

			FillValuesFromSettings(ApplicationSettings.GetDefaultSettings());
		}

		private void FillValuesFromSettings(ApplicationSettings settings)
		{
			FloorDepth = settings.DistanceToFloor;
			MinObjHeight = settings.MinObjHeight;
			OutputPath = settings.OutputPath;
			SampleCount = settings.SampleCount;
			UseAreaMask = settings.UseAreaMask;
			MaskPolygonControlVm = new MaskPolygonControlVm(settings.WorkingAreaContour);
		}
	}
}