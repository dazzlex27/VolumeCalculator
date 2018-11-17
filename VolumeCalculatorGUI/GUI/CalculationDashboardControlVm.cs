using System;
using System.Timers;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using DeviceIntegrations.Scales;
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

		private ApplicationSettings _applicationSettings;
		private ColorCameraParams _colorCameraParams;
		private DepthCameraParams _depthCameraParams;

		private DepthMapProcessor _processor;
		private VolumeCalculator _volumeCalculator;
		private readonly FrameProvider _frameProvider;
		private CalculationResultFileProcessor _calculationResultFileProcessor;

		private string _objectCode;
		private double _objectWeight;
		private int _objectWidth;
		private int _objectHeight;
		private int _objectLength;
		private long _objectVolume;
		private bool _calculationInProgress;
		private bool _usingManualCodeInput;
		private SolidColorBrush _statusBrush;
		private string _statusText;

		private Timer _measurementTimer;
		private bool _weightReady;
		private bool _waitingForReset;

		public ICommand CalculateVolumeCommand { get; }

		public ICommand CalculateVolumeAltCommand { get; }

		public bool CodeReady => !string.IsNullOrEmpty(ObjectCode);

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

		public long ObjectVolume
		{
			get => _objectVolume;
			set
			{
				if (_objectVolume == value)
					return;

				_objectVolume = value;
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

		public CalculationDashboardControlVm(ILogger logger, FrameProvider frameProvider, ApplicationSettings settings, 
			ColorCameraParams colorCameraParams, DepthCameraParams depthCameraParams)
		{
			_logger = logger;
			_frameProvider = frameProvider;
			_applicationSettings = settings;
			_colorCameraParams = colorCameraParams;
			_depthCameraParams = depthCameraParams;

			_calculationResultFileProcessor = new CalculationResultFileProcessor(settings.OutputPath);

			_processor = new DepthMapProcessor(_logger, _colorCameraParams, _depthCameraParams);
			_processor.SetDeviceSettings(_applicationSettings);

			CalculateVolumeCommand = new CommandHandler(CalculateObjectVolume, !CalculationInProgress);
			CalculateVolumeAltCommand = new CommandHandler(CalculateObjectVolumeRgb, !CalculationInProgress);

			CreateAutoStartTimer(settings.TimeToStartMeasurementMs);

			SetStatusReady();
		}

		public void Dispose()
		{
			if (_volumeCalculator != null && _volumeCalculator.IsRunning)
				_processor.Dispose();
		}

		public void ApplicationSettingsUpdated(ApplicationSettings settings)
		{
			_applicationSettings = settings;
			_processor.SetDeviceSettings(settings);
			_calculationResultFileProcessor = new CalculationResultFileProcessor(settings.OutputPath);

			CreateAutoStartTimer(settings.TimeToStartMeasurementMs);
		}

		public void DeviceParamsUpdated(ColorCameraParams colorCameraParams, DepthCameraParams depthCameraParams)
		{
			_colorCameraParams = colorCameraParams;
			_depthCameraParams = depthCameraParams;
			Dispose();

			_processor = new DepthMapProcessor(_logger, _colorCameraParams, _depthCameraParams);
			_processor.SetDeviceSettings(_applicationSettings);
		}

		public void UpdateCodeText(string text)
		{
			if (UsingManualCodeInput || CalculationInProgress)
				return;

			Dispatcher.Invoke(() => { ObjectCode = text; });
			UpdateAutoStartTimer();
		}

		public void UpdateScalesMeasurementData(ScaleMeasurementData data)
		{
			if (CalculationInProgress)
				return;

			Dispatcher.Invoke(() => { ObjectWeight = data.WeightKg; });
			if (data.Status == MeasurementStatus.Ready || data.Status == MeasurementStatus.Measuring)
			{
				Dispatcher.Invoke(() => 
				{
					ObjectLength = 0;
					ObjectWidth = 0;
					ObjectHeight = 0;
					ObjectVolume = 0;
				});
			}

			_weightReady = data.Status == MeasurementStatus.Measured;
			if (data.Status == MeasurementStatus.Ready && _waitingForReset)
				SetStatusReady();

			UpdateAutoStartTimer();
		}

		public DepthMapProcessor GetDepthMapProcessor()
		{
			return _processor;
		}

		private void UpdateAutoStartTimer()
		{
			if (_waitingForReset)
				return;

			if (CodeReady && _weightReady)
			{
				_measurementTimer.Start();
				SetStatusAutoStarting();
			}
			else
				_measurementTimer.Stop();
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
				ObjectVolume = ObjectLength * ObjectWidth * ObjectHeight;
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
				var calculationResult = new CalculationResult(DateTime.Now, ObjectCode, ObjectWeight, ObjectLength,
					ObjectWidth, ObjectHeight, ObjectVolume);
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

		private void SetStatusReady()
		{
			_waitingForReset = false;
			_weightReady = false;

			Dispatcher.Invoke(() =>
			{
				StatusBrush = new SolidColorBrush(Colors.Green);
				StatusText = "Готов к измерению";
			});
		}

		private void SetStatusError()
		{
			Dispatcher.Invoke(() =>
			{
				CalculationInProgress = false;
				StatusBrush = new SolidColorBrush(Colors.Red);
				StatusText = "Произошла ошибка";
			});
		}

		private void SetStatusAutoStarting()
		{
			Dispatcher.Invoke(() =>
			{
				StatusBrush = new SolidColorBrush(Colors.Blue);
				StatusText = "Запущен автотаймер...";
			});
		}

		private void SetStatusInProgress()
		{
			Dispatcher.Invoke(() =>
			{
				CalculationInProgress = true;
				StatusBrush = new SolidColorBrush(Colors.DarkOrange);
				StatusText = "Выполнется измерение...";
			});
		}

		private void SetStatusFinished()
		{
			_waitingForReset = true;

			Dispatcher.Invoke(() =>
			{
				CalculationInProgress = false;
				StatusBrush = new SolidColorBrush(Colors.DarkGreen);
				StatusText = "Измерение завершено";
			});
		}
	}
}