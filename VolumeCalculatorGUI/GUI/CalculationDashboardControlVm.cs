using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Timers;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using DeviceIntegrations.Scales;
using DeviceIntegrations.Scanners;
using FrameProcessor;
using FrameProviders;
using Primitives;
using Primitives.Logging;
using VolumeCalculatorGUI.GUI.Utils;

namespace VolumeCalculatorGUI.GUI
{
	internal class CalculationDashboardControlVm : BaseViewModel, IDisposable
	{
		private readonly ILogger _logger;
		private readonly FrameProvider _frameProvider;
		private readonly IReadOnlyList<IBarcodeScanner> _scanners;
		private readonly IScales _scales;

		private ApplicationSettings _applicationSettings;
		private ColorCameraParams _colorCameraParams;
		private DepthCameraParams _depthCameraParams;

		private DepthMapProcessor _processor;
		private VolumeCalculator _volumeCalculator;
		private CalculationResultFileProcessor _calculationResultFileProcessor;

		private string _objectCode;
		private double _objectWeight;
		private uint _unitCount;
		private int _objectWidth;
		private int _objectHeight;
		private int _objectLength;
		private double _objectVolume;
		private string _comment;
		private bool _calculationInProgress;
		private bool _usingManualCodeInput;
		private SolidColorBrush _statusBrush;
		private string _statusText;

		private bool _codeBoxFocused;
		private bool _unitCountBoxFocused;
		private bool _commentBoxFocused;

		private readonly Timer _autoStartingCheckingTimer;
		private Timer _measurementTimer;
		private bool _waitingForReset;
		private DateTime _calculationFinishTime;

		public ICommand CalculateVolumeCommand { get; }

		public ICommand CalculateVolumeAltCommand { get; }

		public ICommand ResetWeightCommand { get; }

		public ICommand OpenResultsFileCommand { get; }

		public ICommand OpenPhotosFolderCommand { get; }

		public bool CanAcceptBarcodes
		{
			get
			{
				var boxedAreInactive = !UsingManualCodeInput && !CodeBoxFocused && !CommentBoxFocused && !UnitCountBoxFocused;
				return boxedAreInactive && !CalculationInProgress;
			}
		}

		public string ObjectCode
		{
			get => _objectCode;
			set
			{
				if (_objectCode == value)
					return;

				_objectCode = value;
				OnPropertyChanged();
			}
		}

		public double ObjectWeight
		{
			get => _objectWeight;
			set
			{
				if (Math.Abs(_objectWeight - value) < 0.001)
					return;
				_objectWeight = value;

				OnPropertyChanged();
			}
		}

		public uint UnitCount
		{
			get => _unitCount;
			set
			{
				if (_unitCount == value)
					return;

				_unitCount = value;
				OnPropertyChanged();
			}
		}

		public int ObjectLength
		{
			get => _objectLength;
			set
			{
				if (_objectLength == value)
					return;

				_objectLength = value;
				OnPropertyChanged();
			}
		}

		public int ObjectWidth
		{
			get => _objectWidth;
			set
			{
				if (_objectWidth == value)
					return;

				_objectWidth = value;
				OnPropertyChanged();
			}
		}

		public int ObjectHeight
		{
			get => _objectHeight;
			set
			{
				if (_objectHeight == value)
					return;

				_objectHeight = value;
				OnPropertyChanged();
			}
		}

		public double ObjectVolume
		{
			get => _objectVolume;
			set
			{
				if (Math.Abs(_objectVolume - value) < 0.0001)
					return;

				_objectVolume = value;
				OnPropertyChanged();
			}
		}

		public string Comment
		{
			get => _comment;
			set
			{
				if (_comment == value)
					return;

				_comment = value;
				OnPropertyChanged();
			}
		}

		public bool CalculationInProgress
		{
			get => _calculationInProgress;
			set
			{
				if (_calculationInProgress == value)
					return;

				_calculationInProgress = value;
				OnPropertyChanged();
			}
		}

		public bool UsingManualCodeInput
		{
			get => _usingManualCodeInput;
			set
			{
				if (_usingManualCodeInput == value)
					return;

				_usingManualCodeInput = value;
				OnPropertyChanged();
			}
		}

		public SolidColorBrush StatusBrush
		{
			get => _statusBrush;
			set
			{
				if (Equals(_statusBrush, value))
					return;

				_statusBrush = value;
				OnPropertyChanged();
			}
		}

		public string StatusText
		{
			get => _statusText;
			set
			{
				if (_statusText == value)
					return;

				_statusText = value;
				OnPropertyChanged();
			}
		}

		public bool CodeBoxFocused
		{
			get => _codeBoxFocused;
			set
			{
				if (_codeBoxFocused == value)
					return;

				_codeBoxFocused = value;
				OnPropertyChanged();
			}
		}

		public bool UnitCountBoxFocused
		{
			get => _unitCountBoxFocused;
			set
			{
				if (_unitCountBoxFocused == value)
					return;

				_unitCountBoxFocused = value;
				OnPropertyChanged();
			}
		}

		public bool CommentBoxFocused
		{
			get => _commentBoxFocused;
			set
			{
				if (_commentBoxFocused == value)
					return;

				_commentBoxFocused = value;
				OnPropertyChanged();
			}
		}

		private bool CodeReady => !string.IsNullOrEmpty(ObjectCode);

		private bool WeightReady => CurrentWeighingStatus == MeasurementStatus.Measured;

		private MeasurementStatus CurrentWeighingStatus { get; set; }

		public CalculationDashboardControlVm(ILogger logger, FrameProvider frameProvider, IReadOnlyList<IBarcodeScanner> scanners, 
			IScales scales, ApplicationSettings settings, ColorCameraParams colorCameraParams, DepthCameraParams depthCameraParams)
		{
			_logger = logger;
			_frameProvider = frameProvider;

			if (scanners != null)
			{
				_scanners = scanners;
				foreach (var scanner in scanners.Where(s => s != null))
					scanner.CharSequenceFormed += OnBarcodeReady;
			}

			if (scales != null)
			{
				_scales = scales;
				_scales.MeasurementReady += OnWeightMeasurementReady;
			}

			_applicationSettings = settings;
			_colorCameraParams = colorCameraParams;
			_depthCameraParams = depthCameraParams;

			_calculationResultFileProcessor = new CalculationResultFileProcessor(_logger, settings.OutputPath);

			_processor = new DepthMapProcessor(_logger, _colorCameraParams, _depthCameraParams);
			_processor.SetProcessorSettings(_applicationSettings);

			CalculateVolumeCommand = new CommandHandler(CalculateObjectVolume, !CalculationInProgress);
			CalculateVolumeAltCommand = new CommandHandler(CalculateObjectVolumeRgb, !CalculationInProgress);
			ResetWeightCommand = new CommandHandler(ResetWeight, !CalculationInProgress);
			OpenResultsFileCommand = new CommandHandler(OpenResultsFile, !CalculationInProgress);
			OpenPhotosFolderCommand = new CommandHandler(OpenPhotosFolder, !CalculationInProgress);

			_autoStartingCheckingTimer = new Timer(1000) {AutoReset = true};
			_autoStartingCheckingTimer.Elapsed += StartAutoTimerIfNecessary;
			_autoStartingCheckingTimer.Start();

			CreateAutoStartTimer(settings.TimeToStartMeasurementMs);

			SetStatusReady();
		}

		public void Dispose()
		{
			if (_volumeCalculator != null && _volumeCalculator.IsRunning)
				_processor.Dispose();

			_autoStartingCheckingTimer?.Dispose();

			if (_scanners != null)
			{
				foreach (var scanner in _scanners.Where(s => s != null))
					scanner.CharSequenceFormed -= OnBarcodeReady;
			}

			if (_scales != null)
				_scales.MeasurementReady -= OnWeightMeasurementReady;
		}

		public void ApplicationSettingsUpdated(ApplicationSettings settings)
		{
			_applicationSettings = settings;
			_processor.SetProcessorSettings(settings);
			_calculationResultFileProcessor = new CalculationResultFileProcessor(_logger, settings.OutputPath);

			CreateAutoStartTimer(settings.TimeToStartMeasurementMs);
		}

		public void DeviceParamsUpdated(ColorCameraParams colorCameraParams, DepthCameraParams depthCameraParams)
		{
			_colorCameraParams = colorCameraParams;
			_depthCameraParams = depthCameraParams;
			Dispose();

			_processor = new DepthMapProcessor(_logger, _colorCameraParams, _depthCameraParams);
			_processor.SetProcessorSettings(_applicationSettings);
		}

		public DepthMapProcessor GetDepthMapProcessor()
		{
			return _processor;
		}

		private void OnBarcodeReady(string code)
		{
			if (!CanAcceptBarcodes || code == string.Empty)
				return;

			Dispatcher.Invoke(() => { ObjectCode = code; });
		}

		private void OnWeightMeasurementReady(ScaleMeasurementData data)
		{
			if (CalculationInProgress || data == null)
				return;

			Dispatcher.Invoke(() =>
			{
				ObjectWeight = data.WeightKg;
				CurrentWeighingStatus = data.Status;
			});
		}

		private void ResetWeight()
		{
			_scales?.ResetWeight();
		}

		private void StartAutoTimerIfNecessary(object sender, ElapsedEventArgs e)
		{
			if (CurrentWeighingStatus == MeasurementStatus.Ready && _waitingForReset)
			{
				var timePassedSinceCalculationFinished = DateTime.Now - _calculationFinishTime;
				if (timePassedSinceCalculationFinished > TimeSpan.FromSeconds(2))
					SetStatusReady();
			}

			var canProceed = !_waitingForReset && CanAcceptBarcodes;
			if (!canProceed)
				return;

			if (CodeReady && WeightReady)
			{
				_measurementTimer.Start();
				SetStatusAutoStarting();
			}
			else
				_measurementTimer.Stop();
		}

		private void OpenResultsFile()
		{
			try
			{
				var resultsFileInfo = new FileInfo(_applicationSettings.ResultsFilePath);
				if (!resultsFileInfo.Exists)
					return;

				IoUtils.OpenFile(_applicationSettings.ResultsFilePath);
			}
			catch (Exception ex)
			{
				_logger.LogException("Failed to open results file", ex);
			}
		}

		private void OpenPhotosFolder()
		{
			try
			{
				var photosDirectoryInfo = new DirectoryInfo(_applicationSettings.PhotosDirectoryPath);
				if (!photosDirectoryInfo.Exists)
					return;

				IoUtils.OpenFile(_applicationSettings.PhotosDirectoryPath);
			}
			catch (Exception ex)
			{
				_logger.LogException("Failed to open photos folder", ex);
			}
		}

		private void CreateAutoStartTimer(long intervalMs)
		{
			if (_measurementTimer != null)
				_measurementTimer.Elapsed -= OnMeasurementTimerElapsed;

			_measurementTimer = new Timer(intervalMs) {AutoReset = false};
			_measurementTimer.Elapsed += OnMeasurementTimerElapsed;
		}

		private void OnMeasurementTimerElapsed(object sender, ElapsedEventArgs e)
		{
			CalculateObjectVolumeInternal(_applicationSettings.UseRgbAlgorithmByDefault);
		}

		private void CalculateObjectVolume()
		{
			CalculateObjectVolumeInternal(false);
		}

		private void CalculateObjectVolumeRgb()
		{
			CalculateObjectVolumeInternal(true);
		}

		private void CalculateObjectVolumeInternal(bool usingRgbData)
		{
			try
			{
				var canRunCalculation = CheckIfPreConditionsAreSatisfied();
				if (!canRunCalculation)
					return;

				SetStatusInProgress();

				_logger.LogInfo($"Starting a volume check, using rgb={usingRgbData}...");

				_volumeCalculator = new VolumeCalculator(_logger, _frameProvider, _processor, _applicationSettings, usingRgbData);
				_volumeCalculator.CalculationFinished += OnCalculationFinished;
			}
			catch (Exception ex)
			{
				_logger.LogException("Failed to start volume calculation", ex);
				MessageBox.Show("Во время обработки произошла ошибка. Информация записана в журнал", "Ошибка",
					MessageBoxButton.OK, MessageBoxImage.Error);

				SetStatusError();
			}
		}

		private bool CheckIfPreConditionsAreSatisfied()
		{
			if (string.IsNullOrEmpty(ObjectCode))
			{
				MessageBox.Show("Введите код объекта", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Exclamation);

				return false;
			}

			if (!_calculationResultFileProcessor.IsResultFileAccessible())
			{
				MessageBox.Show(
					"Пожалуйста убедитесь, что файл с результатами доступен для записи и закрыт, прежде чем выполнять вычисление",
					"Ошибка", MessageBoxButton.OK, MessageBoxImage.Exclamation);
				_logger.LogInfo("Failed to access the result file");

				return false;
			}

			return true;
		}

		private void OnCalculationFinished(ObjectVolumeData result, CalculationStatus status)
		{
			DisposeVolumeCalculator();

			ProcessCalculationResult(result, status);
			WriteObjectDataToFile();
		}

		private void UpdateVolumeData(ObjectVolumeData volumeData)
		{
			Dispatcher.Invoke(() => 
			{
				ObjectLength = volumeData.Length;
				ObjectWidth = volumeData.Width;
				ObjectHeight = volumeData.Height;
				ObjectVolume = ObjectLength * ObjectWidth * ObjectHeight / 1000.0;
			});
		}

		private void DisposeVolumeCalculator()
		{
			if (_volumeCalculator == null)
				return;

			_volumeCalculator.CalculationFinished -= OnCalculationFinished;
			_volumeCalculator.Dispose();
			_volumeCalculator = null;
		}

		private void ProcessCalculationResult(ObjectVolumeData result, CalculationStatus status)
		{
			try
			{
				if (status == CalculationStatus.Sucessful && result != null)
				{
					SetStatusFinished();
					UpdateVolumeData(result);
				}
				else
				{
					SetStatusError();
					var emptyResult = new ObjectVolumeData(0, 0, 0);
					UpdateVolumeData(emptyResult);
				}

				switch (status)
				{
					case CalculationStatus.Error:
					{
						MessageBox.Show("Во время обработки произошла ошибка. Информация записана в журнал",
							"Результат измерения",
							MessageBoxButton.OK, MessageBoxImage.Error);
						_logger.LogError("Volume calculation finished with errors");
						break;
					}
					case CalculationStatus.TimedOut:
					{
						MessageBox.Show(
							"Не удалось собрать указанное количество образцов для измерения, проверьте соединение с устройством",
							"Ошибка", MessageBoxButton.OK, MessageBoxImage.Exclamation);
						_logger.LogError("Failed to acquire enough samples for volume calculation");
						break;
					}
					case CalculationStatus.Undefined:
					{
						SetStatusError();
						MessageBox.Show("Объект не найден", "Результат измерения",
							MessageBoxButton.OK, MessageBoxImage.Information);
						_logger.LogError("No object was found during volume calculation");
						break;
					}
					case CalculationStatus.Aborted:
					{
						MessageBox.Show("Измерение прервано", "Результат измерения",
							MessageBoxButton.OK, MessageBoxImage.Information);
						_logger.LogError("Volume calculation was aborted");
						break;
					}
					case CalculationStatus.Sucessful:
					{
						if (result != null)
							_logger.LogInfo(
								$"Completed a volume check, L={result.Length} W={result.Width} H={result.Height}");
						else
							_logger.LogError("Calculation was successful, but null result was returned");
						break;
					}
					default:
						throw new ArgumentOutOfRangeException(nameof(status), status,
							@"Failed to resolve failed calculation status");
				}
			}
			catch (Exception ex)
			{
				_logger.LogException("Failed to process result data", ex);
			}
		}

		private void WriteObjectDataToFile()
		{
			try
			{
				var calculationResult = new CalculationResult(DateTime.Now, ObjectCode, ObjectWeight, UnitCount, ObjectLength,
					ObjectWidth, ObjectHeight, ObjectVolume, Comment);
				_calculationResultFileProcessor.WriteCalculationResult(calculationResult);

				ObjectCode = string.Empty;
			}
			catch (Exception ex)
			{
				MessageBox.Show(
					"Не удалось записать результат измерений в файл, проверьте доступность файла и повторите измерения",
					"Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
				_logger.LogException(
					$@"Failed to write calculated values to {_calculationResultFileProcessor.FullOutputPath})",
					ex);
			}
		}

		private void ResetMeasurementValues()
		{
			ObjectLength = 0;
			ObjectWidth = 0;
			ObjectHeight = 0;
			ObjectVolume = 0;
		}

		private void SetStatusReady()
		{
			_waitingForReset = false;

			Dispatcher.Invoke(() =>
			{
				StatusBrush = new SolidColorBrush(Colors.Green);
				StatusText = "Готов к измерению";
				ResetMeasurementValues();
			});
		}

		private void SetStatusError()
		{
			Dispatcher.Invoke(() =>
			{
				CalculationInProgress = false;
				StatusBrush = new SolidColorBrush(Colors.Red);
				StatusText = "Произошла ошибка";
				ResetMeasurementValues();
			});
		}

		private void SetStatusAutoStarting()
		{
			Dispatcher.Invoke(() =>
			{
				StatusBrush = new SolidColorBrush(Colors.Blue);
				StatusText = "Запущен автотаймер...";
				ResetMeasurementValues();
			});
		}

		private void SetStatusInProgress()
		{
			Dispatcher.Invoke(() =>
			{
				CalculationInProgress = true;
				StatusBrush = new SolidColorBrush(Colors.DarkOrange);
				StatusText = "Выполняется измерение...";
				ResetMeasurementValues();
			});
		}

		private void SetStatusFinished()
		{
			_waitingForReset = true;
			_calculationFinishTime = DateTime.Now;

			Dispatcher.Invoke(() =>
			{
				CalculationInProgress = false;
				StatusBrush = new SolidColorBrush(Colors.DarkGreen);
				StatusText = "Измерение завершено";
			});
		}
	}
}