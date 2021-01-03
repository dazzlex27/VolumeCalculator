using System;
using System.Threading.Tasks;
using System.Timers;
using DeviceIntegration.Scales;
using FrameProcessor;
using Primitives;
using Primitives.Logging;
using Primitives.Settings;
using ProcessingUtils;

namespace VolumeCalculator.Utils
{
	internal class VolumeCalculationRequestHandler : IDisposable
	{
		private readonly Timer _autoStartingCheckingTimer;
		private readonly IoDeviceManager _deviceManager;
		private readonly DepthMapProcessor _dmProcessor;

		private readonly ILogger _logger;
		private DateTime _calculationTime;

		private volatile MeasurementStatus _currentWeighingStatus;

		private double _currentWeightGr;
		private bool _isLocked;
		private string _lastBarcode;
		private string _lastComment;
		private DashboardStatus _lastDashboardStatus;

		private uint _lastUnitCount;
		private volatile MeasurementStatus _lastWeighingStatus;
		private double _lastWeightGr;
		private int _palletHeightMm;

		private Timer _pendingTimer;

		private bool _requireBarcode;
		private WeightUnits _selectedWeightUnits;
		private ApplicationSettings _settings;
		private bool _subtractPalletValues;

		private VolumeCalculationLogic _volumeCalculator;

		public VolumeCalculationRequestHandler(ILogger logger, DepthMapProcessor dmProcessor,
			IoDeviceManager deviceManager)
		{
			_logger = logger;
			_dmProcessor = dmProcessor;
			_deviceManager = deviceManager;

			_autoStartingCheckingTimer = new Timer(200) {AutoReset = true};
			_autoStartingCheckingTimer.Elapsed += UpdateAutoTimerStatus;
			_autoStartingCheckingTimer.Start();
		}

		private bool CalculationRunning { get; set; }

		private bool CodeReady
		{
			get
			{
				if (_requireBarcode)
					return !string.IsNullOrEmpty(_lastBarcode);

				return true;
			}
		}

		private bool CanRunAutoTimer => CodeReady && WeightReady && !_isLocked && !WaitingForReset && !CalculationRunning;

		private bool WeightReady => _lastWeighingStatus == MeasurementStatus.Measured && _currentWeightGr > 0.001;

		private bool WaitingForReset => _lastDashboardStatus == DashboardStatus.Finished;

		public void Dispose()
		{
			_autoStartingCheckingTimer.Dispose();
		}

		public event Action<CalculationResultData> CalculationFinished;
		public event Action<string, string> ErrorOccured;
		public event Action<DashboardStatus> DashStatusUpdated;
		public event Action<CalculationStatus, string> CalculationStatusChanged;

		public void StartCalculation(CalculationRequestData data)
		{
			if (CalculationRunning)
			{
				_logger.LogInfo("tried to start a calculation while another one was running");
				return;
			}

			CalculationRunning = true;

			if (_pendingTimer.Enabled)
				_pendingTimer.Stop();

			Task.Run(() =>
			{
				try
				{
					SetDashboardStatus(DashboardStatus.InProgress);
					CalculationStatusChanged?.Invoke(CalculationStatus.Running, "");

					_calculationTime = DateTime.Now;
					if (data != null)
					{
						_lastUnitCount = data.UnitCount;
						_lastComment = data.Comment;
						_lastBarcode = data.Barcode;
					}

					var preConditionEvaluationResult = CheckIfPreConditionsAreSatisfied();
					if (preConditionEvaluationResult != ErrorCode.None)
					{
						var status = GetCalculationStatus(preConditionEvaluationResult, out var errorMessage);

						CalculationStatusChanged?.Invoke(status, errorMessage);
						CalculationFinished?.Invoke(new CalculationResultData(null, status, null));
						SetDashboardStatus(DashboardStatus.Error);
						CalculationRunning = false;
						return;
					}

					_lastWeightGr = _currentWeightGr;
					_lastWeighingStatus = _currentWeighingStatus;

					var activeWorkArea = _settings.AlgorithmSettings.WorkArea;
					_dmProcessor.SetWorkAreaSettings(_settings.AlgorithmSettings.WorkArea);

					var dm1Enabled = activeWorkArea.EnableDmAlgorithm && activeWorkArea.UseDepthMask;
					var dm2Enabled = activeWorkArea.EnablePerspectiveDmAlgorithm && activeWorkArea.UseDepthMask;
					var rgbEnabled = activeWorkArea.EnableRgbAlgorithm && activeWorkArea.UseColorMask;

					_logger.LogInfo(
						$"Starting a volume calculation... dm={dm1Enabled} dm2={dm2Enabled} rgb={rgbEnabled}");

					var calculationIndex = IoUtils.GetCurrentUniversalObjectCounter();
					var cutOffDepth = (short) (activeWorkArea.FloorDepth - activeWorkArea.MinObjectHeight);

					var calculationData = new VolumeCalculationData(_settings.AlgorithmSettings.SampleDepthMapCount,
						_lastBarcode, calculationIndex, dm1Enabled, dm2Enabled, rgbEnabled,
						_settings.GeneralSettings.PhotosDirectoryPath, cutOffDepth);

					_volumeCalculator = new VolumeCalculationLogic(_logger, _dmProcessor, _deviceManager.FrameProvider,
						_deviceManager.RangeMeter, _deviceManager.IpCamera, calculationData);
					_volumeCalculator.CalculationFinished += OnCalculationFinished;
				}
				catch (Exception ex)
				{
					_logger.LogException("Failed to start volume calculation", ex);
					SetDashboardStatus(DashboardStatus.Error);
					CalculationStatusChanged?.Invoke(CalculationStatus.Error, "Неизвестная ошибка");
					CalculationRunning = false;
				}
			});
		}

		private static CalculationStatus GetCalculationStatus(ErrorCode preConditionEvaluationResult,
			out string errorMessage)
		{
			var status = CalculationStatus.Undefined;
			errorMessage = "";

			switch (preConditionEvaluationResult)
			{
				case ErrorCode.BarcodeNotEntered:
					status = CalculationStatus.BarcodeNotEntered;
					errorMessage = "Введите код объекта";
					break;
				case ErrorCode.FileHandleOpen:
					status = CalculationStatus.FailedToCloseFiles;
					errorMessage = "Открыт файл результатов";
					break;
				case ErrorCode.WeightNotStable:
					status = CalculationStatus.WeightNotStable;
					errorMessage = "Вес нестабилен";
					break;
			}

			return status;
		}

		public void UpdateSettings(ApplicationSettings settings)
		{
			if (CalculationRunning)
			{
				_logger.LogError("Tried to assign settings while a clculation was running");
				return;
			}

			_settings = settings;
			_requireBarcode = settings.AlgorithmSettings.RequireBarcode;
			_selectedWeightUnits = settings.AlgorithmSettings.SelectedWeightUnits;

			_subtractPalletValues = settings.AlgorithmSettings.EnablePalletSubtraction;
			_palletHeightMm = settings.AlgorithmSettings.PalletHeightMm;
			CreateAutoStartTimer(settings.AlgorithmSettings.EnableAutoTimer,
				settings.AlgorithmSettings.TimeToStartMeasurementMs);
		}

		public void CancelPendingCalculation()
		{
			var timerEnabled = _pendingTimer != null && _pendingTimer.Enabled;
			if (!timerEnabled)
				return;

			_pendingTimer.Stop();
			SetDashboardStatus(DashboardStatus.Ready);
		}

		public void UpdateLockingStatus(bool isLocked)
		{
			_isLocked = isLocked;
		}

		public void UpdateBarcode(string barcode)
		{
			if (CalculationRunning)
				return;

			_lastBarcode = barcode;
		}

		public void UpdateWeight(ScaleMeasurementData data)
		{
			if (CalculationRunning || data == null)
				return;

			_currentWeightGr = data.WeightGr;
			_currentWeighingStatus = data.Status;
		}

		public void ValidateDashboardStatus()
		{
			SetDashboardStatus(DashboardStatus.Ready);
		}

		private void CreateAutoStartTimer(bool timerEnabled, long intervalMs)
		{
			if (_pendingTimer != null)
				_pendingTimer.Elapsed -= OnMeasurementTimerElapsed;

			if (timerEnabled)
			{
				_pendingTimer = new Timer(intervalMs) {AutoReset = false};
				_pendingTimer.Elapsed += OnMeasurementTimerElapsed;
			}
			else
			{
				_pendingTimer = null;
			}
		}

		private void OnMeasurementTimerElapsed(object sender, ElapsedEventArgs e)
		{
			StartCalculation(null);
		}

		private void SetDashboardStatus(DashboardStatus status)
		{
			_lastDashboardStatus = status;
			DashStatusUpdated?.Invoke(status);
		}

		private void OnCalculationFinished(ObjectVolumeData result, CalculationStatus status, ImageData objectPhoto)
		{
			try
			{
				_logger.LogInfo("Calculation finished, processing results...");

				var correctedUnitCount = _subtractPalletValues ? Math.Max(_lastUnitCount / 2, 1) : _lastUnitCount;
				var correctedLength =
					(int) (_subtractPalletValues ? result.LengthMm / correctedUnitCount : result.LengthMm);
				var correctedWidth =
					(int) (_subtractPalletValues ? result.WidthMm / correctedUnitCount : result.WidthMm);
				var correctedHeight = _subtractPalletValues ? result.HeightMm - _palletHeightMm : result.HeightMm;
				var correctedVolume = correctedLength * correctedWidth * correctedHeight;

				var calculationResult = new CalculationResult(_calculationTime, _lastBarcode, _lastWeightGr,
					_selectedWeightUnits, _lastUnitCount, correctedLength, correctedWidth, correctedHeight,
					correctedVolume, _lastComment, _subtractPalletValues);
				var calculationResultData = new CalculationResultData(calculationResult, status, objectPhoto);

				_lastBarcode = "";
				_calculationTime = DateTime.MinValue;
				_lastWeightGr = 0.0;

				UpdateVisualsWithResult(calculationResultData);

				CalculationFinished?.Invoke(calculationResultData);

				_volumeCalculator.CalculationFinished -= OnCalculationFinished;
				_volumeCalculator = null;

				_logger.LogInfo("Done processing calculation results");
			}
			catch (Exception ex)
			{
				_logger.LogException("Failed to finalize calculation results", ex);
			}
			finally
			{
				CalculationRunning = false;
			}
		}

		private ErrorCode CheckIfPreConditionsAreSatisfied()
		{
			if (!CodeReady)
			{
				_logger.LogInfo("barcode was required, but not entered");
				return ErrorCode.BarcodeNotEntered;
			}

			if (_currentWeighingStatus != MeasurementStatus.Measured)
			{
				_logger.LogInfo("Weight was not stabilized");
				return ErrorCode.WeightNotStable;
			}

			var killedProcess = IoUtils.KillProcess("Excel");
			if (!killedProcess)
			{
				_logger.LogInfo("Failed to access the result file");
				return ErrorCode.FileHandleOpen;
			}

			return ErrorCode.None;
		}

		private void UpdateAutoTimerStatus(object sender, ElapsedEventArgs e)
		{
			if (_lastWeighingStatus == MeasurementStatus.Ready && WaitingForReset)
				SetDashboardStatus(DashboardStatus.Ready);

			if (_pendingTimer == null)
				return;

			if (_pendingTimer.Enabled)
			{
				if (_lastDashboardStatus != DashboardStatus.Pending)
					SetDashboardStatus(DashboardStatus.Pending);

				if (CanRunAutoTimer)
					return;

				_pendingTimer.Stop();
				SetDashboardStatus(DashboardStatus.Ready);
			}
			else
			{
				if (_lastDashboardStatus == DashboardStatus.Pending)
					SetDashboardStatus(DashboardStatus.Ready);

				if (!CanRunAutoTimer || _lastDashboardStatus == DashboardStatus.Pending)
					return;

				_pendingTimer.Start();
				SetDashboardStatus(DashboardStatus.Pending);
			}
		}

		private void UpdateVisualsWithResult(CalculationResultData resultData)
		{
			try
			{
				var calculationResult = resultData.Result;
				CalculationStatusChanged?.Invoke(resultData.Status, "");

				SetDashboardStatus(resultData.Status == CalculationStatus.Successful
					? DashboardStatus.Finished
					: DashboardStatus.Error);

				var status = resultData.Status;

				switch (status)
				{
					case CalculationStatus.Error:
					{
						CalculationStatusChanged?.Invoke(CalculationStatus.Error, "ошибка измерения");
						SetDashboardStatus(DashboardStatus.Error);
						_logger.LogError("Volume calculation finished with errors");
						break;
					}
					case CalculationStatus.TimedOut:
					{
						CalculationStatusChanged?.Invoke(CalculationStatus.Error, "нарушена связь с устройством");
						SetDashboardStatus(DashboardStatus.Error);
						_logger.LogError("Failed to acquire enough samples for volume calculation");
						break;
					}
					case CalculationStatus.Undefined:
					{
						SetDashboardStatus(DashboardStatus.Error);
						_logger.LogError("No object was found during volume calculation");
						break;
					}
					case CalculationStatus.AbortedByUser:
					{
						CalculationStatusChanged?.Invoke(CalculationStatus.Error, "измерение прервано");
						SetDashboardStatus(DashboardStatus.Error);
						_logger.LogError("Volume calculation was aborted");
						break;
					}
					case CalculationStatus.Successful:
					{
						_logger.LogInfo(
							$"Completed a volume check, L={calculationResult.ObjectLengthMm} W={calculationResult.ObjectWidthMm} H={calculationResult.ObjectHeightMm}");
						break;
					}
					case CalculationStatus.FailedToSelectAlgorithm:
					{
						CalculationStatusChanged?.Invoke(CalculationStatus.Error, "не удалось выбрать алгоритм");
						SetDashboardStatus(DashboardStatus.Error);
						break;
					}
					case CalculationStatus.ObjectNotFound:
					{
						CalculationStatusChanged?.Invoke(CalculationStatus.Error, "объект не найден");
						SetDashboardStatus(DashboardStatus.Error);
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
	}
}