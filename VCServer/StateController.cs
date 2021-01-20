using System;
using System.Timers;
using DeviceIntegration.IoCircuits;
using DeviceIntegration.RangeMeters;
using Primitives;

namespace VCServer
{
	public class StateController : IDisposable
	{
		private readonly IIoCircuit _circuit;
		private readonly IRangeMeter _rangeMeter;
		private readonly Timer _laserUpdateTimer;
		
		private DashboardStatus _dashStatus;

		public StateController(IIoCircuit circuit, IRangeMeter rangeMeter)
		{
			_circuit = circuit;
			_rangeMeter = rangeMeter;

			if (_rangeMeter == null)
				return;
			
			_laserUpdateTimer = new Timer(TimeSpan.FromSeconds(120).TotalMilliseconds) {AutoReset = true};
			_laserUpdateTimer.Elapsed += OnLaserUpdateTimerElapsed;
		}
		
		public void Dispose()
		{
			_laserUpdateTimer?.Dispose();
		}

		public void Update(CalculationStatus status)
		{
			var dashStatus = StatusUtils.GetDashboardStatus(status);
			_dashStatus = dashStatus;
			switch (dashStatus)
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
		}
		
		private void UpdateLaser(bool enable)
		{
			_rangeMeter?.ToggleLaser(enable);
			//_circuit?.ToggleRelay(1, enable);
		}

		private void SetStatusReady()
		{
			ToggleReady();
			UpdateLaser(true);
		}

		private void SetStatusError()
		{
			UpdateLaser(true);
			ToggleError();
		}

		private void SetStatusAutoStarting()
		{
			ToggleMeasuring();
			UpdateLaser(false);
		}

		private void SetStatusInProgress()
		{
			ToggleMeasuring();
			UpdateLaser(false);
		}

		private void SetStatusFinished()
		{
			UpdateLaser(true);
			ToggleMeasuring();
		}

		private void OnLaserUpdateTimerElapsed(object sender, ElapsedEventArgs e)
		{
			var needLaserOn = _dashStatus == DashboardStatus.Error || _dashStatus == DashboardStatus.Ready ||
							_dashStatus == DashboardStatus.Finished;
			
			if (needLaserOn)
				UpdateLaser(true);
		}
		
		private void ToggleReady()
		{
			_circuit?.ToggleRelay(2, false);
			_circuit?.ToggleRelay(3, true);
			_circuit?.ToggleRelay(4, true);
		}

		private void ToggleError()
		{
			_circuit?.ToggleRelay(2, true);
			_circuit?.ToggleRelay(3, true);
			_circuit?.ToggleRelay(4, false);
		}

		private void ToggleMeasuring()
		{
			_circuit?.ToggleRelay(2, true);
			_circuit?.ToggleRelay(3, false);
			_circuit?.ToggleRelay(4, true);
		}
	}
}