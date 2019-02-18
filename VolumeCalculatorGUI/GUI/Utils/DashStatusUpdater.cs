using System;
using System.Timers;
using System.Windows.Media;
using DeviceIntegration.IoCircuits;
using DeviceIntegration.RangeMeters;
using DeviceIntegration.Scales;
using Primitives.Logging;
using VolumeCalculatorGUI.Utils;

namespace VolumeCalculatorGUI.GUI.Utils
{
	internal class DashStatusUpdater : IDisposable
	{
		private readonly TimeSpan _laserUpdateTimeSpan = TimeSpan.FromSeconds(10);

		private readonly ILogger _logger;
		private readonly IIoCircuit _circuit;
		private readonly IRangeMeter _rangeMeter;
		private readonly CalculationDashboardControlVm _vm;
		private readonly Timer _autoStartingCheckingTimer;
		private readonly LightToggler _lightToggler;

		private DashboardStatus _dashStatus;

		private DateTime _lastUpdatedLaser;

		public DashboardStatus DashStatus
		{
			get => _dashStatus;
			set
			{
				_dashStatus = value;
				switch (_dashStatus)
				{
					case DashboardStatus.Ready:
						SetStatusReady();
						return;
					case DashboardStatus.Pending:
						SetStatusAutoStarting();
						return;
					case DashboardStatus.InProgress:
						SetStatusInProgress();
						return;
					case DashboardStatus.Finished:
						SetStatusFinished();
						return;
					case DashboardStatus.Error:
						SetStatusError();
						return;
				}
			}
		}

		public string LastErrorMessage { get; set; }

		public Timer PendingTimer { get; set; }

		public DashStatusUpdater(ILogger logger, DeviceSet deviceSet, CalculationDashboardControlVm vm)
		{
			_logger = logger;
			_circuit = deviceSet?.IoCircuit;
			_rangeMeter = deviceSet?.RangeMeter;
			_vm = vm;
			if (_circuit != null)
				_lightToggler = new LightToggler(_circuit);

			_autoStartingCheckingTimer = new Timer(100) { AutoReset = true };
			_autoStartingCheckingTimer.Elapsed += UpdateAutoTimerStatus;
			_autoStartingCheckingTimer.Start();
		}

		public void Dispose()
		{
			_autoStartingCheckingTimer.Dispose();
		}

		public void CancelPendingCalculation()
		{
			var timerEnabled = PendingTimer != null && PendingTimer.Enabled;
			if (!timerEnabled)
				return;

			PendingTimer.Stop();
			_vm.ObjectCode = "";
			DashStatus = DashboardStatus.Ready;
		}

		private void UpdateLaser(bool enable)
		{
			_rangeMeter?.ToggleLaser(enable);
			_circuit?.ToggleRelay(1, enable);
			_lastUpdatedLaser = DateTime.Now;
		}

		private void UpdateAutoTimerStatus(object sender, ElapsedEventArgs e)
		{
			if (_vm.CalculationInProgress)
				return;

			if (DateTime.Now > _lastUpdatedLaser + _laserUpdateTimeSpan)
				UpdateLaser(true);

			if (_vm.CurrentWeighingStatus == MeasurementStatus.Ready && _vm.WaitingForReset)
				DashStatus = DashboardStatus.Ready;

			if (PendingTimer == null)
				return;

			if (PendingTimer.Enabled)
			{
				if (DashStatus != DashboardStatus.Pending)
					DashStatus = DashboardStatus.Pending;

				if (_vm.CanRunAutoTimer)
					return;

				PendingTimer.Stop();
				DashStatus = DashboardStatus.Ready;
			}
			else
			{
				if (DashStatus == DashboardStatus.Pending)
					DashStatus = DashboardStatus.Ready;

				if (!_vm.CanRunAutoTimer || _vm.CalculationPending)
					return;

				PendingTimer.Start();
				DashStatus = DashboardStatus.Pending;
			}
		}

		private void SetStatusReady()
		{
			_vm.WaitingForReset = false;

			_vm.Dispatcher.Invoke(() =>
			{
				_vm.StatusBrush = new SolidColorBrush(Colors.Green);
				_vm.StatusText = "Готов к измерению";
				_vm.CalculationPending = false;
			});

			_lightToggler?.ToggleReady();
			UpdateLaser(true);
		}

		private void SetStatusError()
		{
			_vm.Dispatcher.Invoke(() =>
			{
				_vm.ObjectCode = "";
				_vm.CalculationInProgress = false;
				_vm.StatusBrush = new SolidColorBrush(Colors.Red);
				_vm.StatusText = $"Произошла ошибка: {LastErrorMessage}";
				_vm.CalculationPending = false;
			});

			LastErrorMessage = "";

			UpdateLaser(true);
			_lightToggler?.ToggleError();
		}

		private void SetStatusAutoStarting()
		{
			_vm.Dispatcher.Invoke(() =>
			{
				_vm.StatusBrush = new SolidColorBrush(Colors.Blue);
				_vm.StatusText = "Запущен автотаймер...";
				_vm.CalculationPending = true;
			});

			_lightToggler?.ToggleMeasuring();

			UpdateLaser(false);
		}

		private void SetStatusInProgress()
		{
			_vm.Dispatcher.Invoke(() =>
			{
				_vm.CalculationInProgress = true;
				_vm.StatusBrush = new SolidColorBrush(Colors.DarkOrange);
				_vm.StatusText = "Выполняется измерение...";
				_vm.CalculationPending = false;
			});

			_lightToggler?.ToggleMeasuring();

			UpdateLaser(false);
		}

		private void SetStatusFinished()
		{
			_vm.WaitingForReset = true;

			_vm.Dispatcher.Invoke(() =>
			{
				_vm.CalculationInProgress = false;
				_vm.StatusBrush = new SolidColorBrush(Colors.DarkGreen);
				_vm.StatusText = "Измерение завершено";
				_vm.CalculationPending = false;
			});

			UpdateLaser(true);

			_lightToggler?.ToggleReady();
		}
	}
}