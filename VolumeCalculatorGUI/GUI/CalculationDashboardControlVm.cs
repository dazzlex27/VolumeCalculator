using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Timers;
using System.Windows.Input;
using System.Windows.Media;
using DeviceIntegration.Scales;
using ExtIntegration;
using FrameProcessor;
using Primitives;
using Primitives.Logging;
using Primitives.Settings;
using VolumeCalculatorGUI.GUI.Utils;
using VolumeCalculatorGUI.Utils;

namespace VolumeCalculatorGUI.GUI
{
	internal class CalculationDashboardControlVm : BaseViewModel, IDisposable
	{
		private readonly ILogger _logger;
		private readonly DeviceSet _deviceSet;
		private readonly DepthMapProcessor _processor;
		private readonly DashStatusUpdater _dashStatusUpdater;
		private readonly List<IRequestSender> _requestSenders;

		private ApplicationSettings _settings;

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
		private bool _calculationPending;

		private bool _codeBoxFocused;
		private bool _unitCountBoxFocused;
		private bool _commentBoxFocused;

		private bool _maskMode;

		public ICommand RunVolumeCalculationCommand { get; }

		public ICommand ResetWeightCommand { get; }

		public ICommand OpenResultsFileCommand { get; }

		public ICommand OpenPhotosFolderCommand { get; }

		public ICommand CancelPendingCalculationCommand { get; }

		public bool WaitingForReset { get; set; }

		public string ObjectCode
		{
			get => _objectCode;
			set
			{
				if (_objectCode == value)
					return;

				_objectCode = value;
				if (CodeReady)
					ResetMeasurementValues();

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

		public bool CalculationPending
		{
			get => _calculationPending;
			set
			{
				if (_calculationPending == value)
					return;

				_calculationPending = value;
				OnPropertyChanged();
			}
		}

		private bool CanAcceptBarcodes
		{
			get
			{
				var usingManualInput = UsingManualCodeInput || CodeBoxFocused || CommentBoxFocused || UnitCountBoxFocused;
				return !usingManualInput && !CalculationInProgress;
			}
		}

		public bool CanRunAutoTimer => CodeReady && WeightReady && CanAcceptBarcodes && !WaitingForReset;

		public bool CodeReady
		{
			get
			{
				if (_settings?.AlgorithmSettings == null)
					return !string.IsNullOrEmpty(ObjectCode);

				if (_settings.AlgorithmSettings.RequireBarcode)
					return !string.IsNullOrEmpty(ObjectCode);
					
				return true;
			}
		}

		public bool WeightReady => CurrentWeighingStatus == MeasurementStatus.Measured && ObjectWeight > 0.001;

		public MeasurementStatus CurrentWeighingStatus { get; set; }

		public CalculationDashboardControlVm(ILogger logger, ApplicationSettings settings, DeviceSet deviceSet, 
			DepthMapProcessor processor)
		{
			_logger = logger;
			_deviceSet = deviceSet;
			_processor = processor;

			if (_deviceSet.Scanners != null)
			{
				foreach (var scanner in _deviceSet.Scanners.Where(s => s != null))
					scanner.CharSequenceFormed += OnBarcodeReady;
			}

			if (_deviceSet.Scales != null)
				_deviceSet.Scales.MeasurementReady += OnWeightMeasurementReady;

			_settings = settings;

			_calculationResultFileProcessor = new CalculationResultFileProcessor(_logger, settings.IoSettings.OutputPath);

			_dashStatusUpdater = new DashStatusUpdater(_logger, _deviceSet, this) {DashStatus = DashboardStatus.Ready};
			CreateAutoStartTimer(settings.AlgorithmSettings.EnableAutoTimer, settings.AlgorithmSettings.TimeToStartMeasurementMs);

			RunVolumeCalculationCommand = new CommandHandler(RunVolumeCalculation, !CalculationInProgress);
			ResetWeightCommand = new CommandHandler(ResetWeight, !CalculationInProgress);
			OpenResultsFileCommand = new CommandHandler(OpenResultsFile, !CalculationInProgress);
			OpenPhotosFolderCommand = new CommandHandler(OpenPhotosFolder, !CalculationInProgress);
			CancelPendingCalculationCommand = new CommandHandler(_dashStatusUpdater.CancelPendingCalculation, !CalculationInProgress);

			_requestSenders = new List<IRequestSender>();
			CreateRequestSenders();
		}

		public void Dispose()
		{
			_dashStatusUpdater?.Dispose();

			var scanners = _deviceSet.Scanners;
			if (scanners != null)
			{
				foreach (var scanner in scanners.Where(s => s != null))
					scanner.CharSequenceFormed -= OnBarcodeReady;
			}

			var scales = _deviceSet.Scales;
			if (scales != null)
				scales.MeasurementReady -= OnWeightMeasurementReady;

			foreach (var sender in _requestSenders)
				sender.Dispose();
		}

		public void ApplicationSettingsUpdated(ApplicationSettings settings)
		{
			_settings = settings;
			_calculationResultFileProcessor = new CalculationResultFileProcessor(_logger, settings.IoSettings.OutputPath);

			CreateAutoStartTimer(settings.AlgorithmSettings.EnableAutoTimer, settings.AlgorithmSettings.TimeToStartMeasurementMs);
		}

		public void ToggleMaskMode()
		{
			_maskMode = true;
			_logger.LogInfo("Applying additional masks...");
		}

		private void CreateRequestSenders()
		{
			if (_settings.WebRequestSettings.EnableRequests)
				_requestSenders.Add(new HttpRequestSender(_logger, _settings.WebRequestSettings));

			if (_settings.SqlRequestSettings.EnableRequests)
				_requestSenders.Add(new SqlRequestSender(_logger, _settings.SqlRequestSettings));

			foreach (var sender in _requestSenders)
			{
				try
				{
					sender.Connect();
				}
				catch (Exception ex)
				{
					_logger.LogException("Failed to connect to requet destination", ex);
				}
			}
		}

		private void CreateAutoStartTimer(bool timerEnabled, long intervalMs)
		{
			if (_dashStatusUpdater.PendingTimer != null)
				_dashStatusUpdater.PendingTimer.Elapsed -= OnMeasurementTimerElapsed;

			if (timerEnabled)
			{
				_dashStatusUpdater.PendingTimer = new Timer(intervalMs) {AutoReset = false};
				_dashStatusUpdater.PendingTimer.Elapsed += OnMeasurementTimerElapsed;
			}
			else
			{
				_dashStatusUpdater.PendingTimer = null;
			}
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
			_deviceSet?.Scales?.ResetWeight();
		}

		private void OpenResultsFile()
		{
			try
			{
				var resultsFileInfo = new FileInfo(_settings.IoSettings.ResultsFilePath);
				if (!resultsFileInfo.Exists)
					return;

				IoUtils.OpenFile(_settings.IoSettings.ResultsFilePath);
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
				var photosDirectoryInfo = new DirectoryInfo(_settings.IoSettings.PhotosDirectoryPath);
				if (!photosDirectoryInfo.Exists)
					return;

				IoUtils.OpenFile(_settings.IoSettings.PhotosDirectoryPath);
			}
			catch (Exception ex)
			{
				_logger.LogException("Failed to open photos folder", ex);
			}
		}

		private void OnMeasurementTimerElapsed(object sender, ElapsedEventArgs e)
		{
			RunVolumeCalculation();
		}

		private void RunVolumeCalculation()
		{
			if (CalculationInProgress)
			{
				_logger.LogInfo("tried to start calculation while another one was running, " + Environment.StackTrace);
				return;
			}

			try
			{
				var canRunCalculation = CheckIfPreConditionsAreSatisfied();
				if (!canRunCalculation)
					return;

				_dashStatusUpdater.DashStatus = DashboardStatus.InProgress;

				var dm1Enabled = _settings.AlgorithmSettings.EnableDmAlgorithm;
				var dm2Enabled = _settings.AlgorithmSettings.EnablePerspectiveDmAlgorithm;
				var rgbEnabled = _settings.AlgorithmSettings.EnableRgbAlgorithm;
				_logger.LogInfo($"Starting a volume check... dm={dm1Enabled} dm2={dm2Enabled} rgb={rgbEnabled}");

				long measuredDistanse = 0;
				if (_deviceSet?.RangeMeter != null)
				{
					measuredDistanse = _deviceSet.RangeMeter.GetReading();
					if (measuredDistanse <= 0)
						_logger.LogError("Failed to get range reading, will use depth calculation...");
				}

				_volumeCalculator = new VolumeCalculator(_logger, _deviceSet?.FrameProvider, _processor, _settings, measuredDistanse, 
					_maskMode);
				_volumeCalculator.CalculationFinished += OnCalculationFinished;
			}
			catch (Exception ex)
			{
				_logger.LogException("Failed to start volume calculation", ex);
				AutoClosingMessageBox.Show("Во время обработки произошла ошибка. Информация записана в журнал", "Ошибка");

				_dashStatusUpdater.DashStatus = DashboardStatus.Error;
			}
		}

		private bool CheckIfPreConditionsAreSatisfied()
		{
			if (!CodeReady)
			{
				AutoClosingMessageBox.Show("Введите код объекта", "Ошибка");

				return false;
			}

			var killedProcess = IoUtils.KillProcess("Excel");
			if (killedProcess)
				return true;

			AutoClosingMessageBox.Show("Не удалось закрыть файл с результатами, убедитесь, что файл закрыт", "Ошибка");
			_logger.LogInfo("Failed to access the result file");

			return false;
		}

		private void OnCalculationFinished(ObjectVolumeData result, CalculationStatus status)
		{
			_logger.LogInfo("Calculation finished, processing results...");

			DisposeVolumeCalculator();

			UpdateVisualsWithResult(result, status);

			var calculationResult = new CalculationResult(DateTime.Now, ObjectCode, ObjectWeight, UnitCount, ObjectLength,
				ObjectWidth, ObjectHeight, ObjectVolume, Comment);

			WriteObjectDataToFile(calculationResult);
			SendRequests(calculationResult);

			_logger.LogInfo("Done processing calculatiuon results");
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

		private void ResetMeasurementValues()
		{
			ObjectLength = 0;
			ObjectWidth = 0;
			ObjectHeight = 0;
			ObjectVolume = 0;
			UnitCount = 0;
			Comment = "";
		}

		private void UpdateVisualsWithResult(ObjectVolumeData result, CalculationStatus status)
		{
			try
			{
				if (status == CalculationStatus.Sucessful && result != null)
				{
					_dashStatusUpdater.DashStatus = DashboardStatus.Finished;
					UpdateVolumeData(result);
				}
				else
				{
					_dashStatusUpdater.DashStatus = DashboardStatus.Error;
					var emptyResult = new ObjectVolumeData(0, 0, 0);
					UpdateVolumeData(emptyResult);
				}

				switch (status)
				{
					case CalculationStatus.Error:
					{
						_dashStatusUpdater.LastErrorMessage = "ошибка измерения";
						_dashStatusUpdater.DashStatus = DashboardStatus.Error;
						_logger.LogError("Volume calculation finished with errors");
						break;
					}
					case CalculationStatus.TimedOut:
					{
						_dashStatusUpdater.LastErrorMessage = "нарушена связь с устройством";
						_dashStatusUpdater.DashStatus = DashboardStatus.Error;
						_logger.LogError("Failed to acquire enough samples for volume calculation");
						break;
					}
					case CalculationStatus.Undefined:
					{
						_dashStatusUpdater.DashStatus = DashboardStatus.Error;
						_logger.LogError("No object was found during volume calculation");
						break;
					}
					case CalculationStatus.AbortedByUser:
					{
						_dashStatusUpdater.LastErrorMessage = "измерение прервано";
						_dashStatusUpdater.DashStatus = DashboardStatus.Error;
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
					case CalculationStatus.FailedToSelectAlgorithm:
					{
						_dashStatusUpdater.LastErrorMessage = "не удалось выбрать алгоритм";
						_dashStatusUpdater.DashStatus = DashboardStatus.Error;
							break;
					}
					case CalculationStatus.ObjectNotFound:
					{
						_dashStatusUpdater.LastErrorMessage = "объект не найден";
						_dashStatusUpdater.DashStatus = DashboardStatus.Error;
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

		private void WriteObjectDataToFile(CalculationResult calculationResult)
		{
			try
			{
				_calculationResultFileProcessor.WriteCalculationResult(calculationResult);

				ObjectCode = string.Empty;
			}
			catch (Exception ex)
			{
				AutoClosingMessageBox.Show(
					"Не удалось записать результат измерений в файл, проверьте доступность файла и повторите измерения",
					"Ошибка");
				_logger.LogException(
					$@"Failed to write calculated values to {_calculationResultFileProcessor.FullOutputPath})",
					ex);
			}
		}

		private void SendRequests(CalculationResult result)
		{
			foreach (var sender in _requestSenders)
			{
				Task.Run(() =>
				{
					try
					{
						var sent = sender.Send(result);
					}
					catch (Exception ex)
					{
						_logger.LogException($"Failed to send a request!", ex);
					}

				});
			}
		}
	}
}