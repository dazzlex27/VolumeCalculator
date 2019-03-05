using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using System.Timers;
using System.Windows.Input;
using System.Windows.Media;
using DeviceIntegration.Scales;
using ExtIntegration;
using ExtIntegration.RequestHandlers;
using ExtIntegration.RequestSenders;
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

		private ApplicationSettings _settings;

		private VolumeCalculator _volumeCalculator;
		private CalculationResultFileProcessor _calculationResultFileProcessor;

		private List<IRequestSender> _requestSenders;
		private HttpRequestHandler _httpRequestHandler;
		private Queue<HttpListenerContext> _activeHttpListeners;

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

			_dashStatusUpdater = new DashStatusUpdater(_logger, _deviceSet, this) { DashStatus = DashboardStatus.Ready };

			ApplyValuesFromSettings(settings);

			RunVolumeCalculationCommand = new CommandHandler(RunVolumeCalculation, !CalculationInProgress);
			ResetWeightCommand = new CommandHandler(ResetWeight, !CalculationInProgress);
			OpenResultsFileCommand = new CommandHandler(OpenResultsFile, !CalculationInProgress);
			OpenPhotosFolderCommand = new CommandHandler(OpenPhotosFolder, !CalculationInProgress);
			CancelPendingCalculationCommand = new CommandHandler(_dashStatusUpdater.CancelPendingCalculation, !CalculationInProgress);

			CreateIntegrationEntities();
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

			if (_httpRequestHandler != null)
			{
				_httpRequestHandler.CalculationStartRequested -= OnHttpStartRequestReceived;
				_httpRequestHandler.CalculationStartRequestTimedOut -= OnHttpRequestHandlerTimedOut;
				_httpRequestHandler.Dispose();
			}

			foreach (var sender in _requestSenders)
				sender?.Dispose();
		}

		public void ApplicationSettingsUpdated(ApplicationSettings settings)
		{
			_settings = settings;
			ApplyValuesFromSettings(settings);
		}

		public void ToggleMaskMode()
		{
			_maskMode = true;
			_logger.LogInfo("Applying additional masks...");
		}

		private void ApplyValuesFromSettings(ApplicationSettings settings)
		{
			_calculationResultFileProcessor = new CalculationResultFileProcessor(_logger, settings.IoSettings.OutputPath);
			_deviceSet?.RangeMeter?.SetSubtractionValueMm(settings.IoSettings.RangeMeterSubtractionValueMm);
			CreateAutoStartTimer(settings.AlgorithmSettings.EnableAutoTimer, settings.AlgorithmSettings.TimeToStartMeasurementMs);
		}

		private void CreateIntegrationEntities()
		{
			var integrationSettings = _settings.IntegrationSettings;

			if (integrationSettings.HttpRequestSettings.EnableRequests)
			{
				_httpRequestHandler = new HttpRequestHandler(_logger, integrationSettings.HttpHandlerSettings);
				_httpRequestHandler.CalculationStartRequested += OnHttpStartRequestReceived;
				_httpRequestHandler.CalculationStartRequestTimedOut += OnHttpRequestHandlerTimedOut;

				_activeHttpListeners = new Queue<HttpListenerContext>(1);
			}

			_requestSenders = new List<IRequestSender>();

			if (integrationSettings.SqlRequestSettings.EnableRequests)
				_requestSenders.Add(new SqlRequestSender(_logger, integrationSettings.SqlRequestSettings));

			if (integrationSettings.HttpRequestSettings.EnableRequests)
				_requestSenders.Add(new HttpRequestSender(_logger, integrationSettings.HttpRequestSettings));

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

		private void OnHttpRequestHandlerTimedOut()
		{
		}

		private void OnHttpStartRequestReceived(HttpListenerContext obj)
		{
			if (CalculationInProgress)
			{
				_logger.LogError("HTTP start requested while a calculation was already in progress!");
				_httpRequestHandler.Reset(obj, "Calculation already in progress");
				return;
			}

			_logger.LogInfo("Starting volume calculation upon HTTP request...");
			_activeHttpListeners.Enqueue(obj);
			RunVolumeCalculation();
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
				_dashStatusUpdater.DashStatus = DashboardStatus.InProgress;

				var canRunCalculation = CheckIfPreConditionsAreSatisfied();
				if (!canRunCalculation)
				{
					_dashStatusUpdater.DashStatus = DashboardStatus.Ready;
					if (_httpRequestHandler == null)
						return;

					if (!_activeHttpListeners.Any())
						return;

					_httpRequestHandler.Reset(_activeHttpListeners.Dequeue(), "Barcode was not entered");
					_activeHttpListeners.Clear();

					return;
				}

				var dm1Enabled = _settings.AlgorithmSettings.EnableDmAlgorithm;
				var dm2Enabled = _settings.AlgorithmSettings.EnablePerspectiveDmAlgorithm;
				var rgbEnabled = _settings.AlgorithmSettings.EnableRgbAlgorithm;
				_logger.LogInfo($"Starting a volume check... dm={dm1Enabled} dm2={dm2Enabled} rgb={rgbEnabled}");

				long measuredDistanse = 0;
				if (_deviceSet?.RangeMeter != null)
				{
					measuredDistanse = _deviceSet.RangeMeter.GetReading();
					_logger.LogInfo($"Measured distance - {measuredDistanse}");
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
				ShowMessageBox("Введите код объекта", "Ошибка");

				return false;
			}

			var killedProcess = IoUtils.KillProcess("Excel");
			if (killedProcess)
				return true;

			ShowMessageBox("Не удалось закрыть файл с результатами, убедитесь, что файл закрыт", "Ошибка");
			_logger.LogInfo("Failed to access the result file");

			return false;
		}

		private void OnCalculationFinished(ObjectVolumeData result, CalculationStatus status)
		{
			_logger.LogInfo("Calculation finished, processing results...");

			var calculationResult = new CalculationResult(status, DateTime.Now, ObjectCode, ObjectWeight, UnitCount,
				result.Length, result.Width, result.Height, ObjectVolume, Comment);

			DisposeVolumeCalculator();

			UpdateVisualsWithResult(calculationResult);
			WriteObjectDataToFile(calculationResult);
			SendRequests(calculationResult);

			_logger.LogInfo("Done processing calculatiuon results");
		}

		private void UpdateVolumeData(CalculationResult result)
		{
			Dispatcher.Invoke(() => 
			{
				ObjectLength = result.ObjectLengthMm;
				ObjectWidth = result.ObjectWidthMm;
				ObjectHeight = result.ObjectHeightMm;
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

		private void UpdateVisualsWithResult(CalculationResult result)
		{
			try
			{
				if (result != null && result.Status == CalculationStatus.Sucessful)
				{
					_dashStatusUpdater.DashStatus = DashboardStatus.Finished;
					UpdateVolumeData(result);
				}
				else
				{
					_dashStatusUpdater.DashStatus = DashboardStatus.Error;
					UpdateVolumeData(result);
				}

				var status = result.Status;

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
						_logger.LogInfo(
							$"Completed a volume check, L={result.ObjectLengthMm} W={result.ObjectWidthMm} H={result.ObjectHeightMm}");
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
				ShowMessageBox(
					"Не удалось записать результат измерений в файл, проверьте доступность файла и повторите измерения",
					"Ошибка");
				_logger.LogException(
					$@"Failed to write calculated values to {_calculationResultFileProcessor.FullOutputPath})",
					ex);
			}
		}

		private void SendRequests(CalculationResult result)
		{
			if (_httpRequestHandler != null && _activeHttpListeners.Any())
			{
				var httpContext = _activeHttpListeners.Dequeue();
				_httpRequestHandler.SendResponse(httpContext, result);
			}

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
						_logger.LogException("Failed to send a request!", ex);
					}

				});
			}
		}

		private void ShowMessageBox(string message, string caption)
		{
			Dispatcher.Invoke(() => { AutoClosingMessageBox.Show(message, caption); });
		}
	}
}