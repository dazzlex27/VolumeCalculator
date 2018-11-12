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

		private ImageData _latestColorFrame;
		private DepthMap _latestDepthMap;

		private DepthMapProcessor _processor;
		private VolumeCalculator _volumeCalculator;

		private bool _colorFrameReady;
		private bool _depthFrameReady;

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

		public CalculationDashboardControlVm(ILogger logger, ApplicationSettings settings, 
			ColorCameraParams colorCameraParams, DepthCameraParams depthCameraParams)
		{
			_logger = logger;
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

			SetStatusReady();
		}

		public void Dispose()
		{
			if (_volumeCalculator != null && _volumeCalculator.IsActive)
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

		public void ColorFrameArrived(ImageData image)
		{
			_latestColorFrame = image;

			var calculationIsActive = _volumeCalculator != null && _volumeCalculator.IsActive;
			if (!calculationIsActive)
				return;

			_colorFrameReady = true;

			if (!_depthFrameReady)
				return;

			_volumeCalculator.AdvanceCalculation(_latestDepthMap, _latestColorFrame);
			_colorFrameReady = false;
			_depthFrameReady = false;
		}

		public void DepthFrameArrived(DepthMap depthMap)
		{
			_latestDepthMap = depthMap;

			var calculationIsActive = _volumeCalculator != null && _volumeCalculator.IsActive;
			if (!calculationIsActive)
				return;

			_depthFrameReady = true;

			if (!_colorFrameReady)
				return;

			_volumeCalculator.AdvanceCalculation(_latestDepthMap, _latestColorFrame);
			_colorFrameReady = false;
			_depthFrameReady = false;
		}

		public DepthMapProcessor GetDepthMapProcessor()
		{
			return _processor;
		}

		private void ToggleTimer()
		{
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


				CalculationInProgress = true;
				SetStatusInProgress();

				_logger.LogInfo($"Starting a volume check, using rgb={usingRgbData}...");

				SaveDebugData();

				_volumeCalculator = new VolumeCalculator(_logger, _processor, usingRgbData, _applicationSettings.SampleDepthMapCount);
				_volumeCalculator.CalculationFinished += VolumeCalculator_CalculationFinished;
				_volumeCalculator.CalculationCancelled += VolumeCalculator_CalculationCancelled;
			}
			catch (Exception ex)
			{
				_logger.LogException("Failed to start volume calculation", ex);
				MessageBox.Show("Во время обработки произошла ошибка. Информация записана в журнал", "Ошибка",
					MessageBoxButton.OK, MessageBoxImage.Error);

				SetStatusError();
				CalculationInProgress = false;
			}
		}

		private bool CheckIfPreConditionsAreSatisfied()
		{
			if (_latestDepthMap == null || _latestColorFrame == null)
			{
				MessageBox.Show("Нет кадров для обработки!", "Ошибка", MessageBoxButton.OK,
					MessageBoxImage.Exclamation);
				_logger.LogInfo("Attempted a volume check with no maps");

				return false;
			}

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

		private void SaveDebugData()
		{
			try
			{
				Directory.CreateDirectory(Constants.DebugDataDirectoryName);
				var debugDirectoryInfo = new DirectoryInfo(Constants.DebugDataDirectoryName);
				foreach (var file in debugDirectoryInfo.EnumerateFiles())
					file.Delete();

				ImageUtils.SaveImageDataToFile(_latestColorFrame, Constants.DebugColorFrameFilename);

				var cutOffDepth = (short)(_applicationSettings.FloorDepth - _applicationSettings.MinObjectHeight);
				DepthMapUtils.SaveDepthMapImageToFile(_latestDepthMap, Constants.DebugDepthFrameFilename,
					_depthCameraParams.MinDepth, _depthCameraParams.MaxDepth, cutOffDepth);
			}
			catch (Exception ex)
			{
				_logger.LogException("Failed to save debug data", ex);
			}
		}

		private void VolumeCalculator_CalculationCancelled()
		{
			CalculationInProgress = false;

			DisposeVolumeCalculator();

			_logger.LogError("Calculation cancelled on timeout");

			SetStatusError();

			MessageBox.Show(
				"Не удалось собрать указанное количество образцов для измерения, проверьте соединение с устройством",
				"Ошибка", MessageBoxButton.OK, MessageBoxImage.Exclamation);
		}

		private void VolumeCalculator_CalculationFinished(ObjectVolumeData volumeData)
		{
			CalculationInProgress = false;
			DisposeVolumeCalculator();

			if (volumeData == null)
			{
				volumeData = new ObjectVolumeData(0, 0, 0);
				UpdateVolumeData(volumeData);

				SetStatusError();
				MessageBox.Show("Во время обработки произошла ошибка. Информация записана в журнал", "Ошибка",
					MessageBoxButton.OK, MessageBoxImage.Error);
				_logger.LogError("Volume check returned null");
			}
			else
			{
				_logger.LogInfo(
					$"Completed a volume check, L={volumeData.Length} W={volumeData.Width} H={volumeData.Height}");

				SetStatusFinished();
				UpdateVolumeData(volumeData);
			}

			if (volumeData.Length == 0 || volumeData.Width == 0 || volumeData.Height == 0)
			{
				SetStatusError();
				MessageBox.Show("Объект не найден", "Результат вычисления",
					MessageBoxButton.OK, MessageBoxImage.Information);

				return;
			}

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
			_volumeCalculator.CalculationFinished -= VolumeCalculator_CalculationFinished;
			_volumeCalculator.CalculationCancelled -= VolumeCalculator_CalculationCancelled;
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

		private void SetStatusReady()
		{
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
				StatusBrush = new SolidColorBrush(Colors.Red);
				StatusText = "Произошла ошибка";
			});
		}

		private void SetStatusInProgress()
		{
			Dispatcher.Invoke(() =>
			{
				StatusBrush = new SolidColorBrush(Colors.DarkOrange);
				StatusText = "Выполнется измерение";
			});
		}

		private void SetStatusFinished()
		{
			Dispatcher.Invoke(() =>
			{
				StatusBrush = new SolidColorBrush(Colors.DarkGreen);
				StatusText = "Измерение завершено";
			});
		}
	}
}