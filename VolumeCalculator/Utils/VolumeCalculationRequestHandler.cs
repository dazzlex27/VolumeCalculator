using System;
using System.Threading.Tasks;
using System.Timers;
using DeviceIntegration.Scales;
using FrameProcessor;
using Primitives;
using Primitives.Logging;
using Primitives.Settings;
using ProcessingUtils;
using VCServer;

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
		private string _currentBarcode;
		
		private bool _isLocked;
		private string _lastBarcode;
		private string _lastComment;
		private DashboardStatus _currentDashboardStatus;

		private uint _lastUnitCount;
		private double _lastWeightGr;
		private int _palletHeightMm;

		private Timer _pendingTimer;

		private bool _requireBarcode;
		private WeightUnits _selectedWeightUnits;
		private ApplicationSettings _settings;
		private bool _subtractPalletValues;
		
		private VolumeCalculationLogic _volumeCalculator;
		
		private bool _timerWasCancelled;

		public VolumeCalculationRequestHandler(ILogger logger, DepthMapProcessor dmProcessor,
			IoDeviceManager deviceManager)
		{
			_logger = logger;
			_dmProcessor = dmProcessor;
			_deviceManager = deviceManager;

			_autoStartingCheckingTimer = new Timer(200) {AutoReset = true};
			_autoStartingCheckingTimer.Elapsed += RunUpdateRoutine;
			_autoStartingCheckingTimer.Start();
		}

		private bool CalculationRunning { get; set; }

		private bool CodeReady
		{
			get
			{
				if (_requireBarcode)
					return !string.IsNullOrEmpty(_currentBarcode);

				return true;
			}
		}

		private bool CanRunAutoTimer
		{
			get
			{
				var inputDataReady = CodeReady && WeightReady;
				var stateReady = !_isLocked && !WaitingForReset && !_timerWasCancelled;

				return inputDataReady && stateReady && !CalculationRunning;
			}
		}

		private bool WeightReady => _currentWeighingStatus == MeasurementStatus.Measured && _currentWeightGr > 0.001;

		private bool WaitingForReset => _currentDashboardStatus == DashboardStatus.Finished;

		public void Dispose()
		{
			_autoStartingCheckingTimer?.Dispose();
		}

		public event Action<CalculationResultData> CalculationFinished;
		public event Action<string, string> ErrorOccured;
		public event Action<DashboardStatus> DashStatusUpdated;
		public event Action<CalculationStatus, string> CalculationStatusChanged;
		public event Action<string, string> LastAlgorithmUsedChanged;

		public void StartCalculation(CalculationRequestData data)
		{
			if (CalculationRunning)
			{
				_logger.LogInfo("tried to start a calculation while another one was running");
				return;
			}

			CalculationRunning = true;

			if (_pendingTimer != null && _pendingTimer.Enabled)
				_pendingTimer.Stop();

			Task.Run(() =>
			{
				try
				{
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
						var status = GuiUtils.GetCalculationStatus(preConditionEvaluationResult, out var errorMessage);

						CalculationStatusChanged?.Invoke(status, errorMessage);
						CalculationFinished?.Invoke(new CalculationResultData(null, status, null));
						SetDashboardStatus(DashboardStatus.Error);
						CalculationRunning = false;
						return;
					}
					
					SetDashboardStatus(DashboardStatus.InProgress);
					CalculationStatusChanged?.Invoke(CalculationStatus.Running, "");

					if (data == null)
						_lastBarcode = _currentBarcode;
					
					_lastWeightGr = _currentWeightGr;

					var activeWorkArea = _settings.AlgorithmSettings.WorkArea;
					_dmProcessor.SetWorkAreaSettings(_settings.AlgorithmSettings.WorkArea);

					var dm1Enabled = activeWorkArea.EnableDmAlgorithm && activeWorkArea.UseDepthMask;
					var dm2Enabled = activeWorkArea.EnablePerspectiveDmAlgorithm && activeWorkArea.UseDepthMask;
					var rgbEnabled = activeWorkArea.EnableRgbAlgorithm && activeWorkArea.UseColorMask;

					var algStatus = $"dm={dm1Enabled} dm2={dm2Enabled} rgb={rgbEnabled}";
					_logger.LogInfo($"Starting a volume calculation... {algStatus}");

					var calculationIndex = IoUtils.GetCurrentUniversalObjectCounter();
					var cutOffDepth = (short) (activeWorkArea.FloorDepth - activeWorkArea.MinObjectHeight);

					var calculationData = new VolumeCalculationData(_settings.AlgorithmSettings.SampleDepthMapCount,
						_lastBarcode, calculationIndex, dm1Enabled, dm2Enabled, rgbEnabled,
						_settings.GeneralSettings.PhotosDirectoryPath, activeWorkArea.FloorDepth, cutOffDepth, 
						activeWorkArea.RangeMeterCorrectionValueMm);

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

		public void UpdateSettings(ApplicationSettings settings)
		{
			if (CalculationRunning)
			{
				_logger.LogError("Tried to assign settings while a calculation was running");
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

			_timerWasCancelled = true;
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

			_currentBarcode = barcode;
			_timerWasCancelled = false;
		}

		public void UpdateWeight(ScaleMeasurementData data)
		{
			if (CalculationRunning || data == null)
				return;

			if (Math.Abs(_currentWeightGr - data.WeightGr) > 0.1 || data.Status != MeasurementStatus.Measuring)
				_timerWasCancelled = false;	
			
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
				_pendingTimer = null;
		}

		private void OnMeasurementTimerElapsed(object sender, ElapsedEventArgs e)
		{
			StartCalculation(null);
		}

		private void SetDashboardStatus(DashboardStatus status)
		{
			_currentDashboardStatus = status;
			DashStatusUpdated?.Invoke(status);
		}

		private void OnCalculationFinished(VolumeCalculatorResultData resultData)
		{
			try
			{
				var result = resultData.Result;
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
				var calculationResultData = new CalculationResultData(calculationResult, resultData.Status, resultData.ObjectPhoto);

				_lastBarcode = "";
				_calculationTime = DateTime.MinValue;
				_lastWeightGr = 0.0;

				UpdateVisualsWithResult(calculationResultData);

				CalculationFinished?.Invoke(calculationResultData);
				LastAlgorithmUsedChanged?.Invoke(resultData.LastAlgorithmUsed.ToString(), resultData.WasRangeMeterUsed.ToString());
				
				_volumeCalculator.CalculationFinished -= OnCalculationFinished;
				_volumeCalculator = null;
				
				if (resultData.Status != CalculationStatus.Successful)
					_timerWasCancelled = true;

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

		private void RunUpdateRoutine(object sender, ElapsedEventArgs e)
		{
			if (_currentWeighingStatus == MeasurementStatus.Ready && WaitingForReset)
				SetDashboardStatus(DashboardStatus.Ready);

			if (_pendingTimer == null)
				return;

			if (_pendingTimer.Enabled)
			{
				if (_currentDashboardStatus != DashboardStatus.Pending)
					SetDashboardStatus(DashboardStatus.Pending);

				if (CanRunAutoTimer)
					return;

				_pendingTimer.Stop();
				SetDashboardStatus(DashboardStatus.Ready);
			}
			else
			{
				if (_currentDashboardStatus == DashboardStatus.Pending)
					SetDashboardStatus(DashboardStatus.Ready);

				if (!CanRunAutoTimer || _currentDashboardStatus == DashboardStatus.Pending)
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