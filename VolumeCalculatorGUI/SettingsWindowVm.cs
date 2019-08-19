using System;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using FrameProcessor;
using FrameProviders;
using Primitives;
using Primitives.Logging;
using Primitives.Settings;
using VolumeCalculatorGUI.GUI;
using VolumeCalculatorGUI.GUI.Utils;
using VolumeCalculatorGUI.Utils;

namespace VolumeCalculatorGUI
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
		private short _floorDepth;
		private short _minObjHeight;
		private byte _sampleCount;
		private bool _requireBarcode;
		private string _outputPath;

		private bool _enableAutoTimer;
		private long _timeToStartMeasurementMs;
		private bool _hasReceivedAColorImage;
		private bool _hasReceivedDepthMap;
		private int _rangeMeterSubtractionValue;
		private bool _rangeMeterAvailable;
		private WeightUnits _selectedWeightUnits;

		public WriteableBitmap ColorImageBitmap
		{
			get => _colorImageBitmap;
			set => SetField(ref _colorImageBitmap, value, nameof(ColorImageBitmap));
		}

		public WriteableBitmap DepthImageBitmap
		{
			get => _depthImageBitmap;
			set => SetField(ref _depthImageBitmap, value, nameof(DepthImageBitmap));
		}

		public bool UseColorMask
		{
			get => _useColorMask;
			set => SetField(ref _useColorMask, value, nameof(UseColorMask));
		}

		public MaskPolygonControlVm ColorMaskRectangleControlVm
		{
			get => _colorMaskRectangleControlVm;
			set => SetField(ref _colorMaskRectangleControlVm, value, nameof(ColorMaskRectangleControlVm));
		}

		public bool UseDepthMask
		{
			get => _useDepthMask;
			set => SetField(ref _useDepthMask, value, nameof(UseDepthMask));
		}

		public MaskPolygonControlVm DepthMaskPolygonControlVm
		{
			get => _depthMaskPolygonControlVm;
			set => SetField(ref _depthMaskPolygonControlVm, value, nameof(DepthMaskPolygonControlVm));
		}

		public short MinDepth
		{
			get => _minDepth;
			set => SetField(ref _minDepth, value, nameof(MinDepth));
		}

		public short MaxDepth
		{
			get => _maxDepth;
			set => SetField(ref _maxDepth, value, nameof(MaxDepth));
		}

		public short FloorDepth
		{
			get => _floorDepth;
			set => SetField(ref _floorDepth, value, nameof(FloorDepth));
		}

		public short MinObjHeight
		{
			get => _minObjHeight;
			set => SetField(ref _minObjHeight, value, nameof(MinObjHeight));
		}

		public byte SampleCount
		{
			get => _sampleCount;
			set => SetField(ref _sampleCount, value, nameof(SampleCount));
		}

		public bool RequireBarcode
		{
			get => _requireBarcode;
			set => SetField(ref _requireBarcode, value, nameof(RequireBarcode));
		}

		public string OutputPath
		{
			get => _outputPath;
			set => SetField(ref _outputPath, value, nameof(OutputPath));
		}

		public long TimeToStartMeasurementMs
		{
			get => _timeToStartMeasurementMs;
			set => SetField(ref _timeToStartMeasurementMs, value, nameof(TimeToStartMeasurementMs));
		}

		public bool EnableAutoTimer
		{
			get => _enableAutoTimer;
			set => SetField(ref _enableAutoTimer, value, nameof(EnableAutoTimer));
		}

		public WeightUnits SelectedWeightUnits
		{
			get => _selectedWeightUnits;
			set => SetField(ref _selectedWeightUnits, value, nameof(SelectedWeightUnits));
		}

		public int RangeMeterSubtractionValue
		{
			get => _rangeMeterSubtractionValue;
			set => SetField(ref _rangeMeterSubtractionValue, value, nameof(RangeMeterSubtractionValue));
		}

		public bool HasReceivedAColorImage
		{
			get => _hasReceivedAColorImage;
			set => SetField(ref _hasReceivedAColorImage, value, nameof(HasReceivedAColorImage));
		}

		public bool HasReceivedADepthMap
		{
			get => _hasReceivedDepthMap;
			set => SetField(ref _hasReceivedDepthMap, value, nameof(HasReceivedADepthMap));
		}

		public bool RangeMeterAvailable
		{
			get => _rangeMeterAvailable;
			set => SetField(ref _rangeMeterAvailable, value, nameof(RangeMeterAvailable));
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
				oldIoSettings.ScalesPort, oldIoSettings.ScalesMinWeight, oldIoSettings.ActiveScanners, oldIoSettings.ActiveIoCircuitName,
				oldIoSettings.IoCircuitPort, oldIoSettings.ActiveRangeMeterName, RangeMeterSubtractionValue, oldIoSettings.IpCameraSettings, OutputPath, oldIoSettings.ShutDownPcByDefault);

			var newAlgorithmSettings = new AlgorithmSettings(FloorDepth, MinObjHeight, SampleCount, UseColorMask,
				colorMaskPoints, UseDepthMask, depthMaskPoints, EnableAutoTimer, TimeToStartMeasurementMs,
				RequireBarcode, SelectedWeightUnits);

			return new ApplicationSettings(newIoSettings, newAlgorithmSettings, _oldSettings.IntegrationSettings);
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

				AutoClosingMessageBox.Show(
					"Во время вычисления произошла ошибка, автоматический расчёт не был выполнен",
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
			RangeMeterAvailable = settings.IoSettings.ActiveRangeMeterName != "";
			RangeMeterSubtractionValue = settings.IoSettings.RangeMeterSubtractionValueMm;
			SelectedWeightUnits = settings.AlgorithmSettings.SelectedWeightUnits;
		}
	}
}