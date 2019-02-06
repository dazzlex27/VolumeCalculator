using System;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using FrameProcessor;
using FrameProviders;
using Primitives;
using Primitives.Logging;
using Primitives.Settings;
using VolumeCalculatorGUI.GUI.Utils;
using VolumeCalculatorGUI.Utils;

namespace VolumeCalculatorGUI.GUI
{
	internal class SettingsWindowVm : BaseViewModel
	{
		private readonly ILogger _logger;
		private readonly DepthMapProcessor _depthMapProcessor;
		private readonly ApplicationSettings _oldSettings;

		private DepthMap _latestDepthMap;

		private WriteableBitmap _colorImageBitmap;
		private WriteableBitmap _depthImageBitmap;
		private bool _useColorMask;
		private MaskPolygonControlVm _colorMaskRectangleControlVm;
		private bool _useDepthMask;
		private MaskPolygonControlVm _depthMaskPolygonControlVm;
		private short _maxDepth;
		private short _minDepth;
		private short _distanceToFloor;
		private short _minObjHeight;
		private byte _sampleCount;
		private bool _requireBarcode;
		private string _outputPath;

		private bool _enableAutoTimer;
		private long _timeToStartMeasurementMs;
		private bool _hasReceivedAColorImage;
		private bool _hasReceivedDepthMap;

		public WriteableBitmap ColorImageBitmap
		{
			get => _colorImageBitmap;
			set
			{
				if (Equals(_colorImageBitmap, value))
					return;

				_colorImageBitmap = value;
				OnPropertyChanged();
			}
		}

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

		public bool UseColorMask
		{
			get => _useColorMask;
			set
			{
				if (_useColorMask == value)
					return;

				_useColorMask = value;
				OnPropertyChanged();
			}
		}

		public MaskPolygonControlVm ColorMaskRectangleControlVm
		{
			get => _colorMaskRectangleControlVm;
			set
			{
				if (ReferenceEquals(_colorMaskRectangleControlVm, value))
					return;

				_colorMaskRectangleControlVm = value;
				OnPropertyChanged();
			}
		}

		public bool UseDepthMask
		{
			get => _useDepthMask;
			set
			{
				if (_useDepthMask == value)
					return;

				_useDepthMask = value;
				OnPropertyChanged();
			}
		}

		public MaskPolygonControlVm DepthMaskPolygonControlVm
		{
			get => _depthMaskPolygonControlVm;
			set
			{
				if (ReferenceEquals(_depthMaskPolygonControlVm, value))
					return;

				_depthMaskPolygonControlVm = value;
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

		public bool RequireBarcode
		{
			get => _requireBarcode;
			set
			{
				if (value == _requireBarcode)
					return;

				_requireBarcode = value;
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

		public long TimeToStartMeasurementMs
		{
			get => _timeToStartMeasurementMs;
			set
			{
				if (_timeToStartMeasurementMs == value)
					return;

				_timeToStartMeasurementMs = value;
				OnPropertyChanged();
			}
		}

		public bool EnableAutoTimer
		{
			get => _enableAutoTimer;
			set
			{
				if (_enableAutoTimer == value)
					return;

				_enableAutoTimer = value;
				OnPropertyChanged();
			}
		}

		public bool HasReceivedAColorImage
		{
			get => _hasReceivedAColorImage;
			set
			{
				if (_hasReceivedAColorImage == value)
					return;

				_hasReceivedAColorImage = value;
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

		public SettingsWindowVm(ILogger logger, ApplicationSettings settings, DepthCameraParams depthCameraParams, 
			DepthMapProcessor volumeCalculator)
		{
			_oldSettings = settings;
			_logger = logger;
			_depthMapProcessor = volumeCalculator;

			CalculateFloorDepthCommand = new CommandHandler(CalculateFloorDepth, true);
			ResetSettingsCommand = new CommandHandler(ResetSettings, true);

			var oldSettings = settings ?? ApplicationSettings.GetDefaultSettings();
			MinDepth = depthCameraParams.MinDepth;
			MaxDepth = depthCameraParams.MaxDepth;
			FillValuesFromSettings(oldSettings);
		}

		public ApplicationSettings GetSettings()
		{
			var colorMaskPoints = ColorMaskRectangleControlVm.GetPolygonPoints();
			var depthMaskPoints = DepthMaskPolygonControlVm.GetPolygonPoints();

			var oldIoSettings = _oldSettings.IoSettings;
			var newIoSettings = new IoSettings(oldIoSettings.ActiveCameraName, oldIoSettings.ActiveScalesName,
				oldIoSettings.ScalesPort, oldIoSettings.ActiveScanners, oldIoSettings.ActiveIoCircuitName,
				oldIoSettings.IoCircuitPort, OutputPath, oldIoSettings.ShutDownPcByDefault);

			var newAlgorithmSettings = new AlgorithmSettings(FloorDepth, MinObjHeight, SampleCount, UseColorMask,
				colorMaskPoints, UseDepthMask, depthMaskPoints, EnableAutoTimer, TimeToStartMeasurementMs, RequireBarcode);

			return new ApplicationSettings(newIoSettings, newAlgorithmSettings, _oldSettings.WebRequestSettings,
				_oldSettings.SqlRequestSettings);
		}

		public void ColorFrameUpdated(ImageData image)
		{
			HasReceivedAColorImage = true;
			ColorMaskRectangleControlVm.CanEditPolygon = true;

			ColorImageBitmap = GraphicsUtils.GetWriteableBitmapFromImageData(image);
		}

		public void DepthFrameUpdated(DepthMap depthMap)
		{
			HasReceivedADepthMap = true;
			DepthMaskPolygonControlVm.CanEditPolygon = true;
			_latestDepthMap = depthMap;

			var mapCopy = new DepthMap(depthMap);
			var cutOffDepth = (short)(FloorDepth - MinObjHeight);
			DepthMapUtils.FilterDepthMapByDepthtLimit(mapCopy, cutOffDepth);

			var depthMapData = DepthMapUtils.GetColorizedDepthMapData(mapCopy, MinDepth, FloorDepth);
			var depthMapImage = new ImageData(depthMap.Width, depthMap.Height, depthMapData, 1);

			DepthImageBitmap = GraphicsUtils.GetWriteableBitmapFromImageData(depthMapImage);
		}

		private void CalculateFloorDepth()
		{
			try
			{
				if (_latestDepthMap == null)
				{
					AutoClosingMessageBox.Show("Нет кадров для обработки!", "Ошибка");
					_logger.LogInfo("Attempted a volume check with no maps");

					return;
				}

				var floorDepth = _depthMapProcessor.CalculateFloorDepth(_latestDepthMap);
				if (floorDepth <= 0)
					throw new ArgumentException("Floor depth calculation: return a value less than zero");

				FloorDepth = floorDepth;
				_logger.LogInfo($"Caculated floor depth as {floorDepth}mm");
			}
			catch (Exception ex)
			{
				_logger.LogException("Failed to calculate floor depth!", ex);

				AutoClosingMessageBox.Show("Во время вычисления произошла ошибка, автоматический расчёт не был выполнен", 
					"Ошибка");
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
			FloorDepth = settings.AlgorithmSettings.FloorDepth;
			MinObjHeight = settings.AlgorithmSettings.MinObjectHeight;
			OutputPath = settings.IoSettings.OutputPath;
			SampleCount = settings.AlgorithmSettings.SampleDepthMapCount;
			UseColorMask = settings.AlgorithmSettings.UseColorMask;
			ColorMaskRectangleControlVm = new MaskPolygonControlVm(settings.AlgorithmSettings.ColorMaskContour);
			UseDepthMask = settings.AlgorithmSettings.UseDepthMask;
			DepthMaskPolygonControlVm = new MaskPolygonControlVm(settings.AlgorithmSettings.DepthMaskContour);
			EnableAutoTimer = settings.AlgorithmSettings.EnableAutoTimer;
			TimeToStartMeasurementMs = settings.AlgorithmSettings.TimeToStartMeasurementMs;
			RequireBarcode = settings.AlgorithmSettings.RequireBarcode;
		}
	}
}