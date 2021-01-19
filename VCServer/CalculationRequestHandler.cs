using System;
using System.Threading.Tasks;
using System.Timers;
using DeviceIntegration.Scales;
using FrameProcessor;
using Primitives;
using Primitives.Logging;
using Primitives.Settings;
using ProcessingUtils;

namespace VCServer
{
	public class CalculationRequestHandler : IDisposable
	{
		private readonly ILogger _logger;
		private readonly DepthMapProcessor _dmProcessor;
		private readonly IoDeviceManager _deviceManager;
		
		private readonly Timer _autoStartingCheckingTimer;

		private DateTime _calculationTime;

		private volatile MeasurementStatus _currentWeighingStatus;

		private double _currentWeightGr;
		private string _currentBarcode;
		
		private volatile bool _isLocked;
		private string _lastBarcode;
		private string _lastComment;

		private uint _lastUnitCount;
		private double _lastWeightGr;
		private int _palletHeightMm;

		private Timer _pendingTimer;

		private bool _requireBarcode;
		private WeightUnits _selectedWeightUnits;
		private ApplicationSettings _settings;
		private bool _subtractPalletValues;
		
		private VolumeCalculationLogic _volumeCalculator;
		
		private volatile bool _timerWasCancelled;
		private bool _waitingForReset;

		public CalculationRequestHandler(ILogger logger, DepthMapProcessor dmProcessor,
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
				var stateReady = !_isLocked && !_timerWasCancelled;

				return inputDataReady && stateReady && !CalculationRunning;
			}
		}

		private bool WeightReady => _currentWeighingStatus == MeasurementStatus.Measured && _currentWeightGr > 0.001;

		public void Dispose()
		{
			_autoStartingCheckingTimer?.Dispose();
		}

		public event Action<CalculationResultData> CalculationFinished;
		public event Action<CalculationStatus> CalculationStatusChanged;
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
					if (preConditionEvaluationResult != CalculationStatus.Undefined)
					{
						CalculationStatusChanged?.Invoke(preConditionEvaluationResult);
						var resultData = new CalculationResultData(null, preConditionEvaluationResult, null);
						CalculationFinished?.Invoke(resultData);
						CalculationRunning = false;
						return;
					}
					
					CalculationStatusChanged?.Invoke(CalculationStatus.InProgress);

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
					var status = CalculationStatus.FailedToStart;
					CalculationStatusChanged?.Invoke(status);
					CalculationFinished?.Invoke(new CalculationResultData(null, status, null));
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
			CalculationStatusChanged?.Invoke(CalculationStatus.Undefined);
		}

		public void UpdateLockingStatus(bool isLocked)
		{
			_isLocked = isLocked;
		}
		
		public void ValidateStatus()
		{
			CalculationStatusChanged?.Invoke(CalculationStatus.Undefined);
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

			var needToResetTimer = Math.Abs(_currentWeightGr - data.WeightGr) > 1 && _currentWeighingStatus != data.Status;
			if (needToResetTimer)
				_timerWasCancelled = false;
			
			_currentWeightGr = data.WeightGr;
			_currentWeighingStatus = data.Status;
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

		private void OnCalculationFinished(VolumeCalculatorResultData resultData)
		{
			try
			{
				_timerWasCancelled = true;
				
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

				CalculationStatusChanged?.Invoke(resultData.Status);
				CalculationFinished?.Invoke(calculationResultData);
				LastAlgorithmUsedChanged?.Invoke(resultData.LastAlgorithmUsed.ToString(), resultData.WasRangeMeterUsed.ToString());
				
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
				_waitingForReset = true;
			}
		}

		private CalculationStatus CheckIfPreConditionsAreSatisfied()
		{
			if (!CodeReady)
			{
				_logger.LogInfo("barcode was required, but not entered");
				return CalculationStatus.BarcodeNotEntered;
			}

			if (_currentWeighingStatus != MeasurementStatus.Measured)
			{
				_logger.LogInfo("Weight was not stabilized");
				return CalculationStatus.WeightNotStable;
			}

			var killedProcess = IoUtils.KillProcess("Excel");
			if (!killedProcess)
			{
				_logger.LogInfo("Failed to access the result file");
				return CalculationStatus.FailedToCloseFiles;
			}

			return CalculationStatus.Undefined;
		}

		private void RunUpdateRoutine(object sender, ElapsedEventArgs e)
		{
			if (_waitingForReset && _currentWeighingStatus == MeasurementStatus.Ready)
			{
				_waitingForReset = false;
				CalculationStatusChanged?.Invoke(CalculationStatus.Undefined);
			}

			if (_pendingTimer == null)
				return;

			if (CalculationRunning)
			{
				_pendingTimer?.Stop();
				return;
			}
			
			if (_pendingTimer.Enabled)
			{
				CalculationStatusChanged?.Invoke(CalculationStatus.Pending);

				if (CanRunAutoTimer)
					return;

				_pendingTimer.Stop();
				CalculationStatusChanged?.Invoke(CalculationStatus.Undefined);
			}
			else
			{
				if (!CanRunAutoTimer)// || _currentDashboardStatus == DashboardStatus.Pending)
					return;

				_pendingTimer.Start();
				CalculationStatusChanged?.Invoke(CalculationStatus.Pending);
			}
		}
	}
}