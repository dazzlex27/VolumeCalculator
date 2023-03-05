using System;
using System.Timers;
using Primitives;

namespace VCServer
{
	public sealed class DeviceStateUpdater : IDisposable
	{
		private readonly DeviceSet _deviceSet;
		private readonly Timer _laserUpdateTimer;
		
		private DashboardStatus _dashStatus;

		public DeviceStateUpdater(DeviceSet deviceSet)
		{
			_deviceSet = deviceSet;

			if (_deviceSet.RangeMeter == null)
				return;
			
			_laserUpdateTimer = new Timer(TimeSpan.FromSeconds(120).TotalMilliseconds) {AutoReset = true};
			_laserUpdateTimer.Elapsed += OnLaserUpdateTimerElapsed;
			_laserUpdateTimer.Start();
		}
		
		public void Dispose()
		{
			_laserUpdateTimer?.Dispose();
		}

		public void UpdateCalculationStatus(CalculationStatus status)
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

		public void TogglePause(bool pause)
		{
			_deviceSet.Scales?.TogglePause(pause);

			var scanners = _deviceSet.Scanners;
			if (scanners != null && scanners.Count > 0)
			{
				foreach (var scanner in scanners)
					scanner?.TogglePause(pause);
			}
		}

		public void ResetWeight()
		{
			_deviceSet.Scales?.ResetWeight();
		}

		private void UpdateLaser(bool enable)
		{
			_deviceSet.RangeMeter?.ToggleLaser(enable);
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
			_deviceSet.IoCircuit?.ToggleRelay(2, false);
			_deviceSet.IoCircuit?.ToggleRelay(3, true);
			_deviceSet.IoCircuit?.ToggleRelay(4, true);
		}

		private void ToggleError()
		{
			_deviceSet.IoCircuit?.ToggleRelay(2, true);
			_deviceSet.IoCircuit?.ToggleRelay(3, true);
			_deviceSet.IoCircuit?.ToggleRelay(4, false);
		}

		private void ToggleMeasuring()
		{
			_deviceSet.IoCircuit?.ToggleRelay(2, true);
			_deviceSet.IoCircuit?.ToggleRelay(3, false);
			_deviceSet.IoCircuit?.ToggleRelay(4, true);
		}
	}
}
