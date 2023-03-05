using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using DeviceIntegration.Scales;
using GuiCommon;
using Primitives.Logging;
using Primitives.Settings;
using ProcessingUtils;
using VCServer;
using VCClient.GUI;
using VCClient.Utils;

namespace VCClient.ViewModels
{
	internal class MainWindowVm : BaseViewModel, IDisposable
	{
		const string ServerAppName = "VCServer"; // for logging

		private readonly ILogger _logger;
		private readonly ServerComponentsHandler _serverComponentsHandler;
		private readonly HttpClient _httpClient;
		private readonly List<string> _fatalErrorMessages;

		private StreamViewControlVm _streamViewControlVm;
		private CalculationDashboardControlVm _dashboardControlVm;
		private TestDataGenerationControlVm _testDataGenerationControlVm;

		private volatile bool _shutDownInProgress;

		public string WindowTitle => GuiUtils.AppHeaderString;

		public StreamViewControlVm StreamViewControlVm
		{
			get => _streamViewControlVm;
			set => SetField(ref _streamViewControlVm, value, nameof(StreamViewControlVm));
		}

		public CalculationDashboardControlVm CalculationDashboardControlVm
		{
			get => _dashboardControlVm;
			set => SetField(ref _dashboardControlVm, value, nameof(CalculationDashboardControlVm));
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

		public bool ShutDownByDefault
		{
			get
			{
				var settings = _serverComponentsHandler?.GetSettings();
				if (settings == null)
					return true;

				return settings.IoSettings != null && settings.GeneralSettings.ShutDownPcByDefault;
			}
		}

		public ICommand OpenSettingsCommand { get; }

		public ICommand OpenStatusCommand { get; }

		public ICommand OpenConfiguratorCommand { get; }

		public ICommand ShutDownCommand { get; }

		public ICommand StartMeasurementCommand { get; }
		
		public MainWindowVm(ILogger logger)
		{
			try
			{
				_logger = logger;
				_logger?.LogInfo($"Starting up \"{GuiUtils.AppHeaderString}\"...");
				AppDomain.CurrentDomain.UnhandledException += OnUnhandledException;

				_httpClient = new HttpClient();

				var serverLogger = new TxtLogger(ServerAppName, "main");
				_serverComponentsHandler = new ServerComponentsHandler(serverLogger, _httpClient);

				// TODO: AsyncCommand (replace async void)
				_fatalErrorMessages = new List<string>();
				OpenSettingsCommand = new CommandHandler(async () => await OpenSettingsWindowAsync(), true);
				OpenStatusCommand = new CommandHandler(OpenStatusWindow, true);
				OpenConfiguratorCommand = new CommandHandler(OpenConfigurator, true);
				ShutDownCommand = new CommandHandler(() => ShutDown(true, false), true);
				StartMeasurementCommand = new CommandHandler(OnCalculationStartRequested, true);

				_logger?.LogInfo("Application is created");
			}
			catch (Exception ex)
			{
				_logger?.LogException("Failed to start application, terminating...", ex);
				DisplayFatalErrorsAndCloseApplication().RunSynchronously();
			}
		}

		private void OnCalculationStartRequested()
		{
			_serverComponentsHandler?.Calculator?.StartCalculation(null);
		}

		public async Task InitializeAsync()
		{
			try
			{
				_logger.LogInfo("Initializing application...");

				// TODO: splash screen until this finishes
				await InitializeServerComponentsAsync();
				InitializeSubViewModels();

				_logger.LogInfo("Application is initalized");
			}
			catch (Exception ex)
			{
				_logger.LogException("Failed to initialize, terminating...", ex);
				await DisplayFatalErrorsAndCloseApplication();
			}
		}

		public bool ShutDown(bool shutPcDown, bool force)
		{
			if (_shutDownInProgress)
				return true;

			_shutDownInProgress = true;

			try
			{
				if (force)
				{
					_serverComponentsHandler.DisposeSubSystems();
					Process.GetCurrentProcess().Kill();
					return true;
				}

				if (MessageBox.Show("Вы действительно хотите отключить систему?", "Завершение работы",
						MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.No)
					return false;

				_logger?.LogInfo("Disposing the application...");

				Dispose();

				_logger?.LogInfo("Application stopped");

				if (!shutPcDown)
					return true;

				_logger?.LogInfo("Shutting down the system...");
				_serverComponentsHandler.ShutPcDown();

				return true;
			}
			catch (Exception ex)
			{
				_logger.LogException("Failed to close the application", ex);
				return false;
			}
			finally
			{
				_shutDownInProgress = false;
			}
		}

		public void Dispose()
		{
			_serverComponentsHandler.Dispose();
			_httpClient.Dispose();
		}

		private async Task InitializeServerComponentsAsync()
		{
			var logFatalMessage = "";
			var displayFatalMessage = "";
			
			try
			{
				_logger.LogInfo("Reading settings...");

				logFatalMessage = "FATAL: Failed to initialize application settings!";
				displayFatalMessage = "Не удалось инициализировать настройки приложения";

				await _serverComponentsHandler.InitializeSettingsAsync();
				_serverComponentsHandler.ApplicationSettingsChanged += OnApplicationSettingsUpdated;

				_logger.LogInfo("Settings - ok");

				_logger.LogInfo("Initializing IO devices...");

				logFatalMessage = "FATAL: Failed to initialize IO devices!";
				displayFatalMessage = "Не удалось инициализировать внешние устройства";

				var deviceLogger = new TxtLogger(ServerAppName, "devices");
				_serverComponentsHandler.InitializeIoDevicesAsync(deviceLogger);

				_logger.LogInfo("IO devices- ok");

				_logger.LogInfo("Initializing calculation systems...");

				logFatalMessage = "FATAL: Failed to initialize calculation systems!";
				displayFatalMessage = "Не удалось инициализировать логику вычисления";

				_serverComponentsHandler.InitializeCalculationSystems();

				_logger.LogInfo("Calculation systems - ok");

				_logger.LogInfo("Initializing interration systems...");

				logFatalMessage = "FATAL: Failed to initialize integration systems!";
				displayFatalMessage = "Не удалось инициализировать внешние интеграции";

				var integrationLogger = new TxtLogger(ServerAppName, "integration");
				await _serverComponentsHandler.InitializeIntegrationsAsync(integrationLogger);

				_logger.LogInfo("Integration systems - ok");

			}
			catch (Exception ex)
			{
				_logger.LogException(logFatalMessage, ex);
				_fatalErrorMessages.Add(displayFatalMessage);
				throw;
			}
		}

		private void InitializeSubViewModels()
		{
			try
			{
				_logger.LogInfo("Initializing GUI handlers...");

				var settings = _serverComponentsHandler.GetSettings();
				var deviceManager = _serverComponentsHandler.DeviceManager;
				var frameProvider = deviceManager.DeviceSet.FrameProvider;
				var calculator = _serverComponentsHandler.Calculator;

				StreamViewControlVm = new StreamViewControlVm(_logger, settings, frameProvider);
				CalculationDashboardControlVm = new CalculationDashboardControlVm(_logger, settings,
					deviceManager, calculator);
				TestDataGenerationControlVm = new TestDataGenerationControlVm(_logger, settings, frameProvider);

				calculator.ValidateStatus();

				_logger.LogInfo("GUI handlers - ok");
			}
			catch (Exception ex)
			{
				_logger.LogException("FATAL: Failed to initialize GUI!", ex);

				var message = "Не удалось инициализировать основные программные компоненты системы";
				_fatalErrorMessages.Add(message);
				throw;
			}
		}

		private void OnApplicationSettingsUpdated(ApplicationSettings settings)
		{
			_dashboardControlVm.UpdateSettings(settings);
			_streamViewControlVm.UpdateSettings(settings);
			_testDataGenerationControlVm.UpdateSettings(settings);
		}

		private void OnBarcodeReady(string barcode)
		{

			_dashboardControlVm?.UpdateBarcode(barcode);
		}
		
		private void OnWeightMeasurementReady(ScaleMeasurementData data)
		{

			_dashboardControlVm?.UpdateWeight(data);
		}

		private async Task OpenSettingsWindowAsync()
		{
			try
			{
				var dmProcessor = _serverComponentsHandler.DmProcessor;
				var deviceManager = _serverComponentsHandler.DeviceManager;
				var frameProvider = deviceManager.DeviceSet.FrameProvider;
				deviceManager.DeviceStateUpdater.TogglePause(true);

				var oldSettings = _serverComponentsHandler.GetSettings();
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
					_serverComponentsHandler.UpdateApplicationSettings(settings);
					await _serverComponentsHandler.SaveSettingsAsync();
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
				ShutDown(false, true);
			}
			catch (Exception ex)
			{
				_logger.LogException("Failed to launch configurator", ex);
			}
		}

		private void DisposeSubViewModels()
		{
			_logger.LogInfo("Disposing sub view models...");
			
			_streamViewControlVm?.Dispose();
		}

		private void OnUnhandledException(object sender, UnhandledExceptionEventArgs e)
		{
			_logger.LogException("Unhandled exception in application domain occured, app terminates...",
				(Exception)e.ExceptionObject);

			DisplayFatalErrorsAndCloseApplication().RunSynchronously();
		}

		private async Task DisplayFatalErrorsAndCloseApplication()
		{
			var builder = new StringBuilder();
			builder.AppendLine("Произошли критические ошибки:");
			foreach (var error in _fatalErrorMessages)
				builder.AppendLine(error);
			builder.AppendLine();
			builder.AppendLine("Приложение будет закрыто, информация записана в журнал");

			AutoClosingMessageBox.Show(builder.ToString(), "Аварийное завершение", 5000);

			ShutDown(ShutDownByDefault, true);
		}
	}
}
