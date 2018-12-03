using System;
using System.Timers;
using System.Windows.Media;
using DeviceIntegrations.Scales;
using Primitives;
using Primitives.Logging;
using VolumeCalculatorGUI.Utils;

namespace VolumeCalculatorGUI.GUI.Utils
{
	internal class DashStatusUpdater : IDisposable
	{
		private readonly ILogger _logger;
		private readonly CalculationDashboardControlVm _vm;
		private readonly Timer _autoStartingCheckingTimer;

		private DashboardStatus _dashStatus;

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

		public Timer Timer { get; set; }
	

		public DashStatusUpdater(ILogger logger, ApplicationSettings settings, CalculationDashboardControlVm vm)
		{
			_logger = logger;
			_vm = vm;

			_autoStartingCheckingTimer = new Timer(1000) { AutoReset = true };
			_autoStartingCheckingTimer.Elapsed += UpdateAutoTimerStatus;
			_autoStartingCheckingTimer.Start();
		}

		public void Dispose()
		{
			_autoStartingCheckingTimer.Dispose();
		}

		public void CancelPendingCalculation()
		{
			var timerEnabled = Timer != null && Timer.Enabled;
			if (!timerEnabled)
				return;

			Timer.Stop();
			_vm.ObjectCode = "";
			DashStatus = DashboardStatus.Ready;
		}

		private void UpdateAutoTimerStatus(object sender, ElapsedEventArgs e)
		{
			if (_vm.CurrentWeighingStatus == MeasurementStatus.Ready && _vm.WaitingForReset)
				DashStatus = DashboardStatus.Ready;

			if (Timer == null)
				return;

			if (Timer.Enabled)
			{
				if (DashStatus != DashboardStatus.Pending)
					DashStatus = DashboardStatus.Pending;

				if (_vm.CanRunAutoTimer)
					return;

				Timer.Stop();
				DashStatus = DashboardStatus.Ready;
			}
			else
			{
				if (DashStatus == DashboardStatus.Pending)
					DashStatus = DashboardStatus.Ready;

				if (!_vm.CanRunAutoTimer || _vm.CalculationPending)
					return;

				Timer.Start();
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

			_logger.LogInfo("! status ready");
		}

		private void SetStatusError()
		{
			_vm.Dispatcher.Invoke(() =>
			{
				_vm.ObjectCode = "";
				_vm.CalculationInProgress = false;
				_vm.StatusBrush = new SolidColorBrush(Colors.Red);
				_vm.StatusText = "Произошла ошибка";
				_vm.CalculationPending = false;
			});

			_logger.LogInfo("! status error");
		}

		private void SetStatusAutoStarting()
		{
			_vm.Dispatcher.Invoke(() =>
			{
				_vm.StatusBrush = new SolidColorBrush(Colors.Blue);
				_vm.StatusText = "Запущен автотаймер...";
				_vm.CalculationPending = true;
			});

			_logger.LogInfo("! status pending");
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

			_logger.LogInfo("! status in progress");
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

			_logger.LogInfo("! status finished");
		}
	}
}