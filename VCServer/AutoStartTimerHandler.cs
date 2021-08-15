using System;
using System.Timers;
using Primitives.Settings;

namespace VCServer
{
	public class AutoStartTimerHandler : IDisposable
	{
		private readonly Timer _autoStartingCheckingTimer;
		
		private Timer _pendingTimer;
		private bool _timerWasCancelled;

		public AutoStartTimerHandler()
		{
			_autoStartingCheckingTimer = new Timer(200) {AutoReset = true};
			_autoStartingCheckingTimer.Elapsed += RunUpdateRoutine;
			_autoStartingCheckingTimer.Start();
		}
		
		public event Action<bool> TimerStatusChanged;

		public void Dispose()
		{
			_autoStartingCheckingTimer?.Dispose();
		}
		
		public void UpdateSettings(ApplicationSettings settings)
		{
			CreateAutoStartTimer(settings.AlgorithmSettings.EnableAutoTimer,
				settings.AlgorithmSettings.TimeToStartMeasurementMs);
		}

		public void StopTimer()
		{
			if (_pendingTimer != null && _pendingTimer.Enabled)
				_pendingTimer.Stop();
		}
		
		public void CancelPendingCalculation()
		{
			var timerEnabled = _pendingTimer != null && _pendingTimer.Enabled;
			if (!timerEnabled)
				return;

			_timerWasCancelled = true;
			_pendingTimer.Stop();
			TimerStatusChanged?.Invoke(false);
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
		
		private void RunUpdateRoutine(object sender, ElapsedEventArgs e)
		{
			if (_pendingTimer == null)
				return;

			if (_pendingTimer.Enabled)
			{
				if (_timerWasCancelled)
				{
					_pendingTimer.Stop();
					return;
				}
				
				TimerStatusChanged?.Invoke(true);
			}
			else
			{
				if (_timerWasCancelled)// || _currentDashboardStatus == DashboardStatus.Pending)
					return;

				_pendingTimer.Start();
				TimerStatusChanged?.Invoke(true);
			}
		}
	}
}