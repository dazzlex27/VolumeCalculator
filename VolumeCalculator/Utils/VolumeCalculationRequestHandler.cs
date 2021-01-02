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
        public event Action<CalculationResultData> CalculationFinished;
        public event Action<string, string> ErrorOccured;
        public event Action<DashboardStatus> DashStatusUpdated;
        public event Action<string> ErrorMessageUpdated;
        public event Action<CalculationStatus> CalculationStatusChanged;

        private readonly ILogger _logger;
        private readonly DepthMapProcessor _dmProcessor;
        private readonly IoDeviceManager _deviceManager;
        private readonly Timer _autoStartingCheckingTimer;
        
        private VolumeCalculationLogic _volumeCalculator;

        private DashboardStatus _lastDashboardStatus;
        
        private MeasurementStatus _lastWeighingStatus;
        private bool _requireBarcode;
        private DateTime _calculationTime;
        private string _lastBarcode;
        private double _lastWeightGr;
        private uint _lastUnitCount;
        private string _lastComment;
        private bool _subtractPalletValues;
        private int _palletHeightMm;
        private WeightUnits _selectedWeightUnits;
        private ApplicationSettings _settings;
        
        private Timer _pendingTimer;
        private bool _isLocked;

        public bool CalculationRunning => _volumeCalculator != null && _volumeCalculator.IsRunning;

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

        private bool WeightReady => _lastWeighingStatus == MeasurementStatus.Measured && _lastWeightGr > 0.001;

        private bool WaitingForReset => _lastDashboardStatus == DashboardStatus.Finished;

        public VolumeCalculationRequestHandler(ILogger logger, DepthMapProcessor dmProcessor, IoDeviceManager deviceManager)
        {
            _logger = logger;
            _dmProcessor = dmProcessor;
            _deviceManager = deviceManager;
            
            _autoStartingCheckingTimer = new Timer(100) { AutoReset = true };
            _autoStartingCheckingTimer.Elapsed += UpdateAutoTimerStatus;
            _autoStartingCheckingTimer.Start();
        }

        public void Dispose()
        {
	        _autoStartingCheckingTimer.Dispose();
        }

        public void StartCalculation(CalculationRequestData data)
        {
            if (CalculationRunning)
            {
                _logger.LogInfo("tried to start a calculation while another one was running");
                return;
            }
            
            Task.Run(() =>
			{
				try
				{
					SetDashboardStatus(DashboardStatus.InProgress);
					CalculationStatusChanged?.Invoke(CalculationStatus.Running);

					_calculationTime = DateTime.Now;
					if (data != null)
					{
						_lastUnitCount = data.UnitCount;
						_lastComment = data.Comment;
						_lastBarcode = data.Barcode;
					}

					string error;
					var canRunCalculation = CheckIfPreConditionsAreSatisfied(out error);
					if (!canRunCalculation)
					{
						CalculationStatusChanged?.Invoke(CalculationStatus.BarcodeNotEntered);
						ErrorMessageUpdated?.Invoke(error);
						ErrorOccured?.Invoke(error, "Ошибка");
						CalculationFinished?.Invoke(new CalculationResultData(null, CalculationStatus.BarcodeNotEntered, null));
						return;
					}
					
					var algorithmSettings = _settings.AlgorithmSettings;
					
					var dm1Enabled = algorithmSettings.WorkArea.EnableDmAlgorithm && algorithmSettings.WorkArea.UseDepthMask;
					var dm2Enabled = algorithmSettings.WorkArea.EnablePerspectiveDmAlgorithm && algorithmSettings.WorkArea.UseDepthMask;
					var rgbEnabled = algorithmSettings.WorkArea.EnableRgbAlgorithm && algorithmSettings.WorkArea.UseColorMask;
					
					_logger.LogInfo($"Starting a volume calculation... dm={dm1Enabled} dm2={dm2Enabled} rgb={rgbEnabled}");
					
					var calculationIndex = IoUtils.GetCurrentUniversalObjectCounter();
					var cutOffDepth = (short)(algorithmSettings.WorkArea.FloorDepth - algorithmSettings.WorkArea.MinObjectHeight);
					
					var calculationData = new VolumeCalculationData(algorithmSettings.SampleDepthMapCount,
						_lastBarcode, calculationIndex, dm1Enabled, dm2Enabled, rgbEnabled, 
						_settings.IoSettings.PhotosDirectoryPath, cutOffDepth);

					_volumeCalculator = new VolumeCalculationLogic(_logger, _dmProcessor, _deviceManager.FrameProvider, 
						_deviceManager.RangeMeter, _deviceManager.IpCamera, calculationData);
					_volumeCalculator.CalculationFinished += OnCalculationFinished;
				}
				catch (Exception ex)
				{
					_logger.LogException("Failed to start volume calculation", ex);
					ErrorMessageUpdated?.Invoke("Ошибка запуска");
					SetDashboardStatus(DashboardStatus.Error);
					CalculationStatusChanged?.Invoke(CalculationStatus.Error);
				}
			});
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
	        CreateAutoStartTimer(settings.AlgorithmSettings.EnableAutoTimer, settings.AlgorithmSettings.TimeToStartMeasurementMs);
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

	        _lastWeightGr = data.WeightGr;
	        _lastWeighingStatus = data.Status;
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
		        _pendingTimer = new Timer(intervalMs) { AutoReset = false };
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
	        _lastDashboardStatus = status;
	        DashStatusUpdated?.Invoke(status);
        }
        
        private void OnCalculationFinished(ObjectVolumeData result, CalculationStatus status, ImageData objectPhoto)
        {
            _logger.LogInfo("Calculation finished, processing results...");

            var correctedUnitCount = _subtractPalletValues ? Math.Max(_lastUnitCount / 2, 1) : _lastUnitCount;
            var correctedLength = (int) (_subtractPalletValues ? result.LengthMm / correctedUnitCount : result.LengthMm);
            var correctedWidth = (int) (_subtractPalletValues ? result.WidthMm / correctedUnitCount : result.WidthMm); 
            var correctedHeight = _subtractPalletValues ? result.HeightMm - _palletHeightMm : result.HeightMm;
            var correctedVolume = correctedLength * correctedWidth * correctedHeight;
			
            var calculationResult = new CalculationResult(_calculationTime, _lastBarcode, _lastWeightGr, _selectedWeightUnits,
                _lastUnitCount, correctedLength, correctedWidth, correctedHeight, correctedVolume, _lastComment, _subtractPalletValues);
            var calculationResultData = new CalculationResultData(calculationResult, status, objectPhoto);

			_lastBarcode = "";
			_calculationTime = DateTime.MinValue;
			_lastWeightGr = 0.0;

            UpdateVisualsWithResult(calculationResultData);
            
            CalculationFinished?.Invoke(calculationResultData);

            _volumeCalculator = null;

            _logger.LogInfo("Done processing calculatiuon results");
        }
        
        private bool CheckIfPreConditionsAreSatisfied(out string error)
        {
	        error = "";
	        
	        if (!CodeReady)
	        {
		        error = "Введите код объекта";
		        _logger.LogInfo("barcode was required, but not entered");

		        return false;
	        }

	        if (_lastWeighingStatus != MeasurementStatus.Measured)
	        {
		        error = "Вес не стабилизирован";
		        _logger.LogInfo("Weight was not stabilized");
		        
		        return false;
	        }

	        var killedProcess = IoUtils.KillProcess("Excel");
	        if (killedProcess)
		        return true;

	        error = "Не удалось закрыть файл с результатами, убедитесь, что файл закрыт";
	        _logger.LogInfo("Failed to access the result file");

	        return false;
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
				CalculationStatusChanged?.Invoke(resultData.Status);

				SetDashboardStatus(resultData.Status == CalculationStatus.Successful
					? DashboardStatus.Finished
					: DashboardStatus.Error);

				var status = resultData.Status;

				switch (status)
				{
					case CalculationStatus.Error:
						{
							ErrorMessageUpdated?.Invoke("ошибка измерения");
							SetDashboardStatus(DashboardStatus.Error);
							_logger.LogError("Volume calculation finished with errors");
							break;
						}
					case CalculationStatus.TimedOut:
						{
							ErrorMessageUpdated?.Invoke("нарушена связь с устройством");
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
							ErrorMessageUpdated?.Invoke("измерение прервано");
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
							ErrorMessageUpdated?.Invoke("не удалось выбрать алгоритм");
							SetDashboardStatus(DashboardStatus.Error);
							break;
						}
					case CalculationStatus.ObjectNotFound:
						{
							ErrorMessageUpdated?.Invoke("объект не найден");
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