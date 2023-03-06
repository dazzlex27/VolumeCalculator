using System;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using GuiCommon;
using Primitives.Logging;
using Primitives.Settings;
using ProcessingUtils;
using VCServer;
using VCClient.GUI;

namespace VCClient.ViewModels
{
	internal class MainWindowVm : BaseViewModel, IDisposable
	{
		public event Action<bool, bool> ShutDownRequested;
		public event Action CalculationStartRequested;

		private readonly ILogger _logger;
		private readonly ServerComponentsHandler _server;
		private readonly HttpClient _httpClient;

		private StreamViewControlVm _streamViewControlVm;
		private DashboardControlVm _dashboardControlVm;
		private TestDataGenerationControlVm _testDataGenerationControlVm;

		public StreamViewControlVm StreamViewControlVm
		{
			get => _streamViewControlVm;
			set => SetField(ref _streamViewControlVm, value, nameof(StreamViewControlVm));
		}

		public DashboardControlVm DashboardControlVm
		{
			get => _dashboardControlVm;
			set => SetField(ref _dashboardControlVm, value, nameof(DashboardControlVm));
		}

		public TestDataGenerationControlVm TestDataGenerationControlVm
		{
			get => _testDataGenerationControlVm;
			set => SetField(ref _testDataGenerationControlVm, value, nameof(TestDataGenerationControlVm));
		}

		public bool IsTestDataGenerationControlVisible
		{
			get => _testDataGenerationControlVm?.ShowControl ?? false;
			set
			{
				if (_testDataGenerationControlVm == null)
					return;

				if (_testDataGenerationControlVm.ShowControl == value)
					return;

				_testDataGenerationControlVm.ShowControl = value;
				OnPropertyChanged();
			}
		}

		public ICommand OpenSettingsCommand { get; }

		public ICommand OpenStatusCommand { get; }

		public ICommand OpenConfiguratorCommand { get; }

		public ICommand ShutDownCommand { get; }

		public ICommand StartMeasurementCommand { get; }

		public MainWindowVm(ILogger logger, HttpClient httpClient, ServerComponentsHandler server)
		{
			_logger = logger;
			_httpClient = httpClient;
			_server = server;

			OpenSettingsCommand = new CommandHandler(async () => await OpenSettingsWindowAsync(), true);
			OpenStatusCommand = new CommandHandler(OpenStatusWindow, true);
			OpenConfiguratorCommand = new CommandHandler(OpenConfigurator, true);
			ShutDownCommand = new CommandHandler(() => RaiseShutDownRequestedEvent(true, false), true);
			StartMeasurementCommand = new CommandHandler(OnCalculationStartRequested, true);
		}

		public void Dispose()
		{
			_streamViewControlVm?.Dispose();
		}

		public void InitializeSubViewModels()
		{
			var settings = _server.GetSettings();
			var deviceManager = _server.DeviceManager;
			var frameProvider = deviceManager.DeviceSet.FrameProvider;
			var calculator = _server.Calculator;

			StreamViewControlVm?.Dispose();
			StreamViewControlVm = new StreamViewControlVm(_logger, settings, frameProvider);

			DashboardControlVm = new DashboardControlVm(_logger, settings, deviceManager, calculator);
			TestDataGenerationControlVm = new TestDataGenerationControlVm(_logger, settings, frameProvider);
		}

		public void UpdateApplicationSettings(ApplicationSettings settings)
		{
			_dashboardControlVm.UpdateSettings(settings);
			_streamViewControlVm.UpdateSettings(settings);
			_testDataGenerationControlVm.UpdateSettings(settings);
		}

		private async Task OpenSettingsWindowAsync()
		{
			try
			{
				var dmProcessor = _server.DmProcessor;
				var deviceManager = _server.DeviceManager;
				var frameProvider = deviceManager.DeviceSet.FrameProvider;
				deviceManager.DeviceStateUpdater.TogglePause(true);

				var oldSettings = _server.GetSettings();
				var settingsWindowVm = new SettingsWindowVm(_logger, oldSettings,
					frameProvider.GetDepthCameraParams(), dmProcessor);
				frameProvider.ColorFrameReady += settingsWindowVm.ColorFrameUpdated;
				frameProvider.DepthFrameReady += settingsWindowVm.DepthFrameUpdated;

				try
				{
					var settingsWindow = new SettingsWindow
					{
						Owner = Application.Current.Windows.OfType<Window>().SingleOrDefault(x => x.IsActive),
						DataContext = settingsWindowVm
					};

					var settingsChanged = settingsWindow.ShowDialog() == true;
					if (!settingsChanged)
						return;

					var settings = settingsWindowVm.GetSettings();
					_server.UpdateApplicationSettings(settings);
					await _server.SaveSettingsAsync();
				}
				catch (Exception ex)
				{
					_logger.LogException("Error occured while changing settings", ex);
				}
				finally
				{
					deviceManager.DeviceStateUpdater.TogglePause(false);
					frameProvider.ColorFrameReady -= settingsWindowVm.ColorFrameUpdated;
					frameProvider.DepthFrameReady -= settingsWindowVm.DepthFrameUpdated;
				}
			}
			catch (Exception ex)
			{
				_logger.LogException("Exception occured during a settings change", ex);
				AutoClosingMessageBox.Show("Во время задания настроек произошла ошибка. Информация записана в журнал", "Ошибка");
			}
		}

		private void OpenStatusWindow()
		{
			try
			{
				var statusWindowVm = new StatusWindowVm(_logger, _httpClient);
				var statusWindow = new StatusWindow
				{
					Owner = Application.Current.Windows.OfType<Window>().SingleOrDefault(x => x.IsActive),
					DataContext = statusWindowVm
				};

				statusWindow.ShowDialog();
			}
			catch (Exception ex)
			{
				_logger.LogException("Error occured while displaying status window", ex);
			}
		}

		private void OpenConfigurator()
		{
			try
			{
				var message = "Данное действие приведёт к закрытию текущего приложения и открытию приложения настройки, вы хотите продолжить?";

				if (MessageBox.Show(message, "Открытие Конфигуратора", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.No)
					return;

				IoUtils.StartProcess("VCConfigurator.exe", true);
				RaiseShutDownRequestedEvent(false, true);
			}
			catch (Exception ex)
			{
				_logger.LogException("Failed to launch configurator", ex);
			}
		}

		private void OnCalculationStartRequested()
		{
			CalculationStartRequested?.Invoke();
		}

		private void RaiseShutDownRequestedEvent(bool shutDownPc, bool force)
		{
			ShutDownRequested?.Invoke(shutDownPc, force);
		}
	}
}
