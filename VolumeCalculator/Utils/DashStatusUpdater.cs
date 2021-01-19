using System;
using System.Timers;
using DeviceIntegration.IoCircuits;
using DeviceIntegration.RangeMeters;
using Primitives;
using Primitives.Logging;
using VCServer;

namespace VolumeCalculator.Utils
{
	internal class DashStatusUpdater : IDisposable
	{
		private readonly IIoCircuit _circuit;
		private readonly IRangeMeter _rangeMeter;
		private readonly LightToggler _lightToggler;
		private readonly Timer _laserUpdateTimer;

		private DashboardStatus _dashStatus;

		public DashStatusUpdater(ILogger logger, IoDeviceManager deviceManager)
		{
			logger.LogInfo("Creating dash status updater...");

			_circuit = deviceManager.IoCircuit;
			_rangeMeter = deviceManager.RangeMeter;
			if (_circuit != null)
				_lightToggler = new LightToggler(_circuit);

			_laserUpdateTimer = new Timer(TimeSpan.FromSeconds(120).TotalMilliseconds) {AutoReset = true};
			_laserUpdateTimer.Elapsed += OnLaserUpdateTimerElapsed;
		}

		public event Action<DashboardStatus> DashboardStatusChanged;

		public void Dispose()
		{
			_laserUpdateTimer.Dispose();
		}

		public void UpdateCalculationStatus(CalculationStatus status)
		{
			switch (status)
			{
				case CalculationStatus.Successful:
					UpdateDashStatus(DashboardStatus.Finished);
					break;
				case CalculationStatus.InProgress:
					UpdateDashStatus(DashboardStatus.InProgress);
					break;
				case CalculationStatus.Undefined:
					UpdateDashStatus(DashboardStatus.Ready);
					break;
				case CalculationStatus.Pending:
					UpdateDashStatus(DashboardStatus.Pending);
					break;
				case CalculationStatus.CalculationError:
				case CalculationStatus.AbortedByUser:
				case CalculationStatus.TimedOut: 
				case CalculationStatus.BarcodeNotEntered:
				case CalculationStatus.FailedToStart:
				case CalculationStatus.ObjectNotFound:
				case CalculationStatus.WeightNotStable:
				case CalculationStatus.FailedToCloseFiles:
				case CalculationStatus.FailedToSelectAlgorithm:
					UpdateDashStatus(DashboardStatus.Error);
					break;
				default:
					throw new ArgumentOutOfRangeException(nameof(status), status, "failed to parse status");
			}
		}

		private void UpdateDashStatus(DashboardStatus status)
		{
			_dashStatus = status;
			switch (_dashStatus)
			{
				case DashboardStatus.Ready:
					SetStatusReady();
					break;
				case DashboardStatus.Pending:
					SetStatusAutoStarting();
					break;
				case DashboardStatus.InProgress:
					SetStatusInProgress();
					break;
				case DashboardStatus.Finished:
					SetStatusFinished();
					break;
				case DashboardStatus.Error:
					SetStatusError();
					break;
			}

			DashboardStatusChanged?.Invoke(status);
		}

		private void UpdateLaser(bool enable)
		{
			_rangeMeter?.ToggleLaser(enable);
			//_circuit?.ToggleRelay(1, enable);
		}

		private void SetStatusReady()
		{
			_lightToggler?.ToggleReady();
			UpdateLaser(true);
		}

		private void SetStatusError()
		{
			UpdateLaser(true);
			_lightToggler?.ToggleError();
		}

		private void SetStatusAutoStarting()
		{
			_lightToggler?.ToggleMeasuring();
			UpdateLaser(false);
		}

		private void SetStatusInProgress()
		{
			_lightToggler?.ToggleMeasuring();
			UpdateLaser(false);
		}

		private void SetStatusFinished()
		{
			UpdateLaser(true);
			_lightToggler?.ToggleMeasuring();
		}

		private void OnLaserUpdateTimerElapsed(object sender, ElapsedEventArgs e)
		{
			var needLaserOn = _dashStatus == DashboardStatus.Error || _dashStatus == DashboardStatus.Ready ||
			                  _dashStatus == DashboardStatus.Finished;
			
			if (needLaserOn)
				UpdateLaser(true);
		}
	}
}