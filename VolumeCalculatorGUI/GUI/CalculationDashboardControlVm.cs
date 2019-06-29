using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Timers;
using System.Windows.Input;
using System.Windows.Media;
using DeviceIntegration.Scales;
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
		public event Action<CalculationResultData> CalculationFinished;
		public event Action<CalculationStatus> CalculationStatusChanged;

		private event Action CalculationStartRequested;

		private readonly ILogger _logger;
		private readonly DeviceSet _deviceSet;
		private readonly DepthMapProcessor _processor;
		private readonly DashStatusUpdater _dashStatusUpdater;

		private ApplicationSettings _settings;

		private VolumeCalculator _volumeCalculator;
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

		private string _weightLabelText;

		private bool _codeBoxFocused;
		private bool _unitCountBoxFocused;
		private bool _commentBoxFocused;

		private bool _maskMode;

		private DateTime _calculationTime;
		private string _lastBarcode;
		private double _lastWeight;
		private uint _lastUnitCount;
		private string _lastComment;

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
				SetField(ref _objectCode, value, nameof(ObjectCode));

				if (CodeReady)
					ResetMeasurementValues();
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
			set => SetField(ref _unitCount, value, nameof(UnitCount));
		}

		public int ObjectLength
		{
			get => _objectLength;
			set => SetField(ref _objectLength, value, nameof(ObjectLength));
		}

		public int ObjectWidth
		{
			get => _objectWidth;
			set => SetField(ref _objectWidth, value, nameof(ObjectWidth));
		}

		public int ObjectHeight
		{
			get => _objectHeight;
			set => SetField(ref _objectHeight, value, nameof(ObjectHeight));
		}

		public double ObjectVolume
		{
			get => _objectVolume;
			set => SetField(ref _objectVolume, value, nameof(ObjectVolume));
		}

		public string Comment
		{
			get => _comment;
			set => SetField(ref _comment, value, nameof(Comment));
		}

		public bool CalculationInProgress
		{
			get => _calculationInProgress;
			set => SetField(ref _calculationInProgress, value, nameof(CalculationInProgress));
		}

		public bool UsingManualCodeInput
		{
			get => _usingManualCodeInput;
			set => SetField(ref _usingManualCodeInput, value, nameof(UsingManualCodeInput));
		}

		public SolidColorBrush StatusBrush
		{
			get => _statusBrush;
			set => SetField(ref _statusBrush, value, nameof(StatusBrush));
		}

		public string StatusText
		{
			get => _statusText;
			set => SetField(ref _statusText, value, nameof(StatusText));
		}

		public bool CodeBoxFocused
		{
			get => _codeBoxFocused;
			set => SetField(ref _codeBoxFocused, value, nameof(CodeBoxFocused));
		}

		public bool UnitCountBoxFocused
		{
			get => _unitCountBoxFocused;
			set => SetField(ref _unitCountBoxFocused, value, nameof(UnitCountBoxFocused));
		}

		public bool CommentBoxFocused
		{
			get => _commentBoxFocused;
			set => SetField(ref _commentBoxFocused, value, nameof(CommentBoxFocused));
		}

		public bool CalculationPending
		{
			get => _calculationPending;
			set => SetField(ref _calculationPending, value, nameof(CalculationPending));
		}

		public string WeightLabelText
		{
			get => _weightLabelText;
			set => SetField(ref _weightLabelText, value, nameof(WeightLabelText));
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

			_calculationTime = DateTime.Now;
			_lastBarcode = "";
			_lastWeight = 0;
			_lastComment = "";

			RunVolumeCalculationCommand = new CommandHandler(OnCalculationStartRequested, !CalculationInProgress);
			ResetWeightCommand = new CommandHandler(ResetWeight, !CalculationInProgress);
			OpenResultsFileCommand = new CommandHandler(OpenResultsFile, !CalculationInProgress);
			OpenPhotosFolderCommand = new CommandHandler(OpenPhotosFolder, !CalculationInProgress);
			CancelPendingCalculationCommand = new CommandHandler(_dashStatusUpdater.CancelPendingCalculation, !CalculationInProgress);
		}

		public void StartCalculation(CalculationRequestData requestData)
		{
			if (requestData != null)
			{
				ObjectCode = requestData.Barcode;
				UnitCount = requestData.UnitCount;
				Comment = requestData.Comment;
			}

			RunVolumeCalculation();
		}

		private void OnCalculationStartRequested()
		{
			RunVolumeCalculation();
		}

		public void Dispose()
		{
			CalculationStartRequested -= CalculationStartRequested;

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
			_deviceSet?.RangeMeter?.SetSubtractionValueMm(settings.IoSettings.RangeMeterSubtractionValueMm);
			CreateAutoStartTimer(settings.AlgorithmSettings.EnableAutoTimer, settings.AlgorithmSettings.TimeToStartMeasurementMs);

			Dispatcher.Invoke(() =>
			{
				switch (settings.AlgorithmSettings.SelectedWeightUnits)
				{
					case WeightUnits.Gr:
						WeightLabelText = "гр";
						break;
					case WeightUnits.Kg:
						WeightLabelText = "кг";
						break;
					default:
						WeightLabelText = "";
						break;
				}
			});
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
				_dashStatusUpdater.PendingTimer = null;
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
				switch (_settings.AlgorithmSettings.SelectedWeightUnits)
				{
					case WeightUnits.Gr:
						ObjectWeight = data.WeightGr;
						break;
					case WeightUnits.Kg:
						ObjectWeight = data.WeightGr / 1000.0;
						break;
					default:
						ObjectWeight = double.NaN;
						break;
				}

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
			OnCalculationStartRequested();
		}

		private void RunVolumeCalculation()
		{
			if (CalculationInProgress)
			{
				_logger.LogInfo("tried to start calculation while another one was running");
				return;
			}

			Task.Run(() =>
			{
				try
				{
					_dashStatusUpdater.DashStatus = DashboardStatus.InProgress;
					CalculationStatusChanged?.Invoke(CalculationStatus.Running);

					var canRunCalculation = CheckIfPreConditionsAreSatisfied();
					if (!canRunCalculation)
					{
						_dashStatusUpdater.DashStatus = DashboardStatus.Ready;
						CalculationStatusChanged?.Invoke(CalculationStatus.BarcodeNotEntered);
						CalculationFinished?.Invoke(new CalculationResultData(null, CalculationStatus.BarcodeNotEntered,
							null));
						return;
					}

					_calculationTime = DateTime.Now;
					_lastBarcode = ObjectCode;
					_lastWeight = ObjectWeight;
					_lastUnitCount = UnitCount;
					_lastComment = Comment;

					var dm1Enabled = _settings.AlgorithmSettings.EnableDmAlgorithm;
					var dm2Enabled = _settings.AlgorithmSettings.EnablePerspectiveDmAlgorithm;
					var rgbEnabled = _settings.AlgorithmSettings.EnableRgbAlgorithm;
					_logger.LogInfo($"Starting a volume check... dm={dm1Enabled} dm2={dm2Enabled} rgb={rgbEnabled}");

					var measurementNumber = IoUtils.GetCurrentUniversalObjectCounter();

					_volumeCalculator = new VolumeCalculator(_logger, _deviceSet, _processor, _settings, _maskMode, measurementNumber);
					_volumeCalculator.CalculationFinished += OnCalculationFinished;
				}
				catch (Exception ex)
				{
					_logger.LogException("Failed to start volume calculation", ex);
					AutoClosingMessageBox.Show("Во время обработки произошла ошибка. Информация записана в журнал",
						"Ошибка");

					_dashStatusUpdater.DashStatus = DashboardStatus.Error;
					CalculationStatusChanged?.Invoke(CalculationStatus.Error);
				}
			});
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

		private void OnCalculationFinished(ObjectVolumeData result, CalculationStatus status, ImageData objectPhoto)
		{
			_logger.LogInfo("Calculation finished, processing results...");

			var weightUnits = _settings.AlgorithmSettings.SelectedWeightUnits;
			var calculationResult = new CalculationResult(_calculationTime, _lastBarcode, _lastWeight, weightUnits, 
				_lastUnitCount, result.Length, result.Width, result.Height, ObjectVolume, _lastComment);
			var calculationResultData = new CalculationResultData(calculationResult, status, objectPhoto);

			ObjectCode = "";
			UnitCount = 0;
			Comment = "";

			DisposeVolumeCalculator();

			UpdateVisualsWithResult(calculationResultData);

			CalculationFinished?.Invoke(calculationResultData);

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

		private void UpdateVisualsWithResult(CalculationResultData resultData)
		{
			try
			{
				var calculationResult = resultData.Result;
				CalculationStatusChanged?.Invoke(resultData.Status);

				if (resultData.Status == CalculationStatus.Sucessful)
				{
					_dashStatusUpdater.DashStatus = DashboardStatus.Finished;
					UpdateVolumeData(calculationResult);
				}
				else
				{
					_dashStatusUpdater.DashStatus = DashboardStatus.Error;
					UpdateVolumeData(calculationResult);
				}

				var status = resultData.Status;

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
							$"Completed a volume check, L={calculationResult.ObjectLengthMm} W={calculationResult.ObjectWidthMm} H={calculationResult.ObjectHeightMm}");
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

		private void ShowMessageBox(string message, string caption)
		{
			Dispatcher.Invoke(() => { AutoClosingMessageBox.Show(message, caption); });
		}
	}
}