using System;
using System.Globalization;
using System.IO;
using System.Text;
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
using VolumeCalculatorGUI.Utils;

namespace VolumeCalculatorGUI.GUI
{
	internal class CalculationDashboardControlVm : BaseViewModel, IDisposable
	{
		private readonly ILogger _logger;
		private string _resultFullPath;

		private ApplicationSettings _applicationSettings;
		private ColorCameraParams _colorCameraParams;
		private DepthCameraParams _depthCameraParams;

		private DepthMapProcessor _processor;
		private VolumeCalculator _volumeCalculator;
		private readonly FrameProvider _frameProvider;

		private string _objectCode;
		private double _objectWeight;
		private int _objectWidth;
		private int _objectHeight;
		private int _objectLength;
		private long _objectVolume;
		private bool _calculationInProgress;
		private bool _useManualCodeInput;
		private bool _useManualWeightInput;
		private SolidColorBrush _statusBrush;
		private string _statusText;

		private Timer _measurementTimer;
		private bool _codeReady;
		private bool _weightReady;
		private bool _waitingForReset;

		public ICommand CalculateVolumeCommand { get; }

		public ICommand CalculateVolumeAltCommand { get; }

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

		public bool UseManualCodeInput
		{
			get => _useManualCodeInput;
			set
			{
				if (_useManualCodeInput == value)
					return;

				_useManualCodeInput = value;
				OnPropertyChanged();
			}
		}

		public bool UseManualWeightInput
		{
			get => _useManualWeightInput;
			set
			{
				if (_useManualWeightInput == value)
					return;

				_useManualWeightInput = value;
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

			_processor = new DepthMapProcessor(_logger, _colorCameraParams, _depthCameraParams);
			_processor.SetDeviceSettings(_applicationSettings);

			_resultFullPath = IoUtils.GetFullResultFilePath(_applicationSettings.OutputPath);

			CalculateVolumeCommand = new CommandHandler(CalculateObjectVolume, !CalculationInProgress);
			CalculateVolumeAltCommand = new CommandHandler(CalculateObjectVolumeRgb, !CalculationInProgress);

			CreateMeasurementTimer(settings.TimeToStartMeasurementMs);
			_codeReady = false;
			_weightReady = false;
			_waitingForReset = false;

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
			_resultFullPath = IoUtils.GetFullResultFilePath(_applicationSettings.OutputPath);

			CreateMeasurementTimer(settings.TimeToStartMeasurementMs);
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
			if (UseManualCodeInput || CalculationInProgress)
				return;

			Dispatcher.Invoke(() => { ObjectCode = text; });
			_codeReady = text != "";
			ToggleTimer();
		}

		public void UpdateScalesMeasurementData(ScaleMeasurementData data)
		{
			if (UseManualWeightInput || CalculationInProgress)
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
			if (data.Status == MeasurementStatus.Ready)
				SetStatusReady();

			ToggleTimer();
		}

		public DepthMapProcessor GetDepthMapProcessor()
		{
			return _processor;
		}

		private void ToggleTimer()
		{
			if (_waitingForReset)
				return;

			if (_codeReady && _weightReady)
				_measurementTimer.Start();
			else
			{
				_measurementTimer.Stop();
				_logger.LogInfo("Stopped measurement timer because one or both params have changed");
			}
		}

		private void CreateMeasurementTimer(long intervalMs)
		{
			if (_measurementTimer != null)
				_measurementTimer.Elapsed -= OnMeasurementTimerElapsed;

			_measurementTimer = new Timer(intervalMs);
			_measurementTimer.Elapsed += OnMeasurementTimerElapsed;
		}

		private void OnMeasurementTimerElapsed(object sender, ElapsedEventArgs e)
		{
			CalculateObjectVolumeInternal(_applicationSettings.UseRgbAlgorithmByDefault);
			_codeReady = false;
			_weightReady = false;
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

			if (!IsResultFileAccessible())
			{
				MessageBox.Show(
					"Пожалуйста убедитесь, что файл с результатами доступен для записи и закрыт, прежде чем выполнять вычисление",
					"Ошибка", MessageBoxButton.OK, MessageBoxImage.Exclamation);
				_logger.LogInfo("Failed to access the result file");

				return false;
			}

			return true;
		}

		private void VolumeCalculator_CalculationCancelled()
		{
			DisposeVolumeCalculator();

			_logger.LogError("Calculation cancelled on timeout");

			SetStatusError();

			MessageBox.Show(
				"Не удалось собрать указанное количество образцов для измерения, проверьте соединение с устройством",
				"Ошибка", MessageBoxButton.OK, MessageBoxImage.Exclamation);
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
			_volumeCalculator.CalculationFinished -= OnCalculationFinished;
			_volumeCalculator.Dispose();
			_volumeCalculator = null;
		}

		private void WriteHeadersToCsv()
		{
			Directory.CreateDirectory(_applicationSettings.OutputPath);
			using (var resultFile = new StreamWriter(_resultFullPath, true, Encoding.Default))
			{
				var resultString = new StringBuilder();
				resultString.Append("#");
				resultString.Append($@"{Constants.CsvSeparator}date local");
				resultString.Append($@"{Constants.CsvSeparator}time local");
				resultString.Append($@"{Constants.CsvSeparator}code");
				resultString.Append($@"{Constants.CsvSeparator}weight (kg)");
				resultString.Append($@"{Constants.CsvSeparator}length (mm)");
				resultString.Append($@"{Constants.CsvSeparator}width (mm)");
				resultString.Append($@"{Constants.CsvSeparator}height (mm)");
				resultString.Append($@"{Constants.CsvSeparator}volume (mm^2)");
				resultFile.WriteLine(resultString);
				resultFile.Flush();
				_logger.LogInfo($@"Created the csv at {_resultFullPath} and wrote the headers to it");
			}
		}

		private bool IsResultFileAccessible()
		{
			try
			{
				if (!File.Exists(_resultFullPath))
				{
					WriteHeadersToCsv();
					return true;
				}

				using (Stream stream = new FileStream(_resultFullPath, FileMode.Open, FileAccess.ReadWrite, FileShare.None))
				{
					stream.ReadByte();
				}

				return true;
			}
			catch (IOException)
			{
				return false;
			}
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
				var safeName = ObjectCode;
				if (!string.IsNullOrEmpty(safeName))
				{
					var nameWithoutReturns = ObjectCode.Replace(Environment.NewLine, " ");
					safeName = nameWithoutReturns.Replace(Constants.CsvSeparator, " ");
				}

				var safeWeight = ObjectWeight.ToString(CultureInfo.InvariantCulture);

				Directory.CreateDirectory(_applicationSettings.OutputPath);
				using (var resultFile = new StreamWriter(_resultFullPath, true, Encoding.Default))
				{
					var dateTime = DateTime.Now;
					var resultString = new StringBuilder();
					resultString.Append(IoUtils.GetNextUniversalObjectCounter());
					resultString.Append($@"{Constants.CsvSeparator}{dateTime.ToShortDateString()}");
					resultString.Append($@"{Constants.CsvSeparator}{dateTime.ToShortTimeString()}");
					resultString.Append($@"{Constants.CsvSeparator}{safeName}");
					resultString.Append($@"{Constants.CsvSeparator}{safeWeight}");
					resultString.Append($@"{Constants.CsvSeparator}{ObjectLength}");
					resultString.Append($@"{Constants.CsvSeparator}{ObjectWidth}");
					resultString.Append($@"{Constants.CsvSeparator}{ObjectHeight}");
					resultString.Append($@"{Constants.CsvSeparator}{ObjectVolume}");
					resultFile.WriteLine(resultString);
					resultFile.Flush();
					_logger.LogInfo("Wrote the calculated values to csv");
				}

				ObjectCode = string.Empty;
			}
			catch (IOException ex)
			{
				MessageBox.Show(
					"Не удалось записать результат измерений в файл, проверьте доступность файла и повторите измерения",
					"Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
				_logger.LogException(
					$@"Failed to write calculated values to {_resultFullPath})",
					ex);
			}
		}

		private void SetStatusReady()
		{
			_waitingForReset = false;

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

		private void SetStatusInProgress()
		{
			Dispatcher.Invoke(() =>
			{
				CalculationInProgress = true;
				StatusBrush = new SolidColorBrush(Colors.DarkOrange);
				StatusText = "Выполнется измерение";
			});
		}

		private void SetStatusFinished()
		{
			Dispatcher.Invoke(() =>
			{
				CalculationInProgress = false;
				StatusBrush = new SolidColorBrush(Colors.DarkGreen);
				StatusText = "Измерение завершено";
			});
		}
	}
}