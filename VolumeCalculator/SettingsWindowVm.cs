using System;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using FrameProcessor;
using FrameProviders;
using GuiCommon;
using Primitives;
using Primitives.Logging;
using Primitives.Settings;
using VolumeCalculator.GUI;

namespace VolumeCalculator
{
	internal class SettingsWindowVm : BaseViewModel
	{
		private readonly ILogger _logger;
		private readonly DepthMapProcessor _depthMapProcessor;
		private readonly ApplicationSettings _oldSettings;

		private DepthMap _latestDepthMap;

		private WriteableBitmap _colorImageBitmap;
		private WriteableBitmap _depthImageBitmap;

		private WorkAreaSettingsVm _workAreaVm;
		private short _maxDepth;
		private short _minDepth;
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
		private bool _enablePalletSubtraction;
		private double _palletWeightKg;
		private int _palletHeightMm;

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

		public WorkAreaSettingsVm WorkAreaVm
		{
			get => _workAreaVm;
			set => SetField(ref _workAreaVm, value, nameof(WorkAreaVm));
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
		
		public bool EnablePalletSubtraction
		{
			get => _enablePalletSubtraction;
			set => SetField(ref _enablePalletSubtraction, value, nameof(EnablePalletSubtraction));
		}
		
		public double PalletWeightKg
		{
			get => _palletWeightKg;
			set => SetField(ref _palletWeightKg, value, nameof(PalletWeightKg));
		}
		
		public int PalletHeightMm
		{
			get => _palletHeightMm;
			set => SetField(ref _palletHeightMm, value, nameof(PalletHeightMm));
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
			var newGeneralSettings = new GeneralSettings(OutputPath, _oldSettings.GeneralSettings.ShutDownPcByDefault);
			
			var oldIoSettings = _oldSettings.IoSettings;
			var newIoSettings = new IoSettings(oldIoSettings.ActiveCameraName, oldIoSettings.ActiveScales,
				oldIoSettings.ActiveScanners, oldIoSettings.ActiveIoCircuit, oldIoSettings.ActiveRangeMeterName, 
				RangeMeterSubtractionValue, oldIoSettings.IpCameraSettings);

			var newWorkAreaSettings = WorkAreaVm.GetSettings();
			var newAlgorithmSettings = new AlgorithmSettings(newWorkAreaSettings, SampleCount, EnableAutoTimer, TimeToStartMeasurementMs,
				RequireBarcode, SelectedWeightUnits, EnablePalletSubtraction, PalletWeightKg * 1000, PalletHeightMm);

			return new ApplicationSettings(newGeneralSettings, newIoSettings, newAlgorithmSettings, _oldSettings.IntegrationSettings);
		}

		public void ColorFrameUpdated(ImageData image)
		{
			HasReceivedAColorImage = true;
			WorkAreaVm.ColorMaskRectangleControlVm.CanEditPolygon = true;

			ColorImageBitmap = GraphicsUtils.GetWriteableBitmapFromImageData(image);
		}

		public void DepthFrameUpdated(DepthMap depthMap)
		{
			HasReceivedADepthMap = true;
			WorkAreaVm.DepthMaskPolygonControlVm.CanEditPolygon = true;
			_latestDepthMap = depthMap;

			var mapCopy = new DepthMap(depthMap);
			var cutOffDepth = (short)(WorkAreaVm.FloorDepth - WorkAreaVm.MinObjHeight);
			DepthMapUtils.FilterDepthMapByDepthtLimit(mapCopy, cutOffDepth);

			var depthMapData = DepthMapUtils.GetColorizedDepthMapData(mapCopy, MinDepth, WorkAreaVm.FloorDepth);
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

				WorkAreaVm.FloorDepth = floorDepth;
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
			WorkAreaVm = new WorkAreaSettingsVm(settings.AlgorithmSettings.WorkArea);
			OutputPath = settings.GeneralSettings.OutputPath;
			SampleCount = settings.AlgorithmSettings.SampleDepthMapCount;
			EnableAutoTimer = settings.AlgorithmSettings.EnableAutoTimer;
			TimeToStartMeasurementMs = settings.AlgorithmSettings.TimeToStartMeasurementMs;
			RequireBarcode = settings.AlgorithmSettings.RequireBarcode;
			RangeMeterAvailable = settings.IoSettings.ActiveRangeMeterName != "";
			RangeMeterSubtractionValue = settings.IoSettings.RangeMeterSubtractionValueMm;
			SelectedWeightUnits = settings.AlgorithmSettings.SelectedWeightUnits;
			EnablePalletSubtraction = settings.AlgorithmSettings.EnablePalletSubtraction;
			PalletWeightKg = settings.AlgorithmSettings.PalletWeightGr / 1000;
			PalletHeightMm = settings.AlgorithmSettings.PalletHeightMm;
		}
	}
}