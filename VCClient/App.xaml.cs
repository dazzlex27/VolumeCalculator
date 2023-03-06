using GuiCommon;
using Primitives.Logging;
using Primitives.Settings;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using VCClient.GUI;
using VCClient.Utils;
using VCClient.ViewModels;
using VCServer;

namespace VCClient
{
	public partial class App
	{
		private readonly HttpClient _httpClient;
		private readonly List<string> _fatalErrorMessages;

		private ILogger _serverLogger;
		private ILogger _deviceLogger;
		private ILogger _integrationLogger;
		private ILogger _clientLogger;

		private ServerComponentsHandler _server;
		private MainWindowVm _mainWindowVm;
		private MainWindow _mainWindow;

		private volatile bool _shutDownInProgress;

		public App()
		{
			_httpClient = new HttpClient();
			_fatalErrorMessages = new List<string>();
		}

		public bool ShutDownByDefault
		{
			get
			{
				var settings = _server?.GetSettings();
				if (settings == null)
					return true;

				return settings.IoSettings != null && settings.GeneralSettings.ShutDownPcByDefault;
			}
		}

		private async void OnApplicationStartup(object sender, StartupEventArgs e)
		{
			const string serverAppTitle = "VCServer";

			try
			{
				_serverLogger = new TxtLogger(serverAppTitle, "main");
				_deviceLogger = new TxtLogger(serverAppTitle, "devices");
				_integrationLogger = new TxtLogger(serverAppTitle, "intergration");
				_clientLogger = new TxtLogger(GuiUtils.AppTitle, "main");

				_serverLogger?.LogInfo($"Starting up \"{serverAppTitle}\"...");
				AppDomain.CurrentDomain.UnhandledException += OnUnhandledException;

				await InitializeServerAsync();
				InitializeClient();

				_server.Calculator.ValidateStatus(); // reset main window
				_mainWindow.Show();
			}
			catch (Exception ex)
			{
				_serverLogger.LogException("Failed to initialize, terminating...", ex);
				DisplayFatalErrorsAndCloseApplication();
			}
		}

		private void OnApplicationExit(object sender, ExitEventArgs e)
		{
			ShutDown(ShutDownByDefault, false);
		}

		private void ShutDown(bool shutPcDown, bool force)
		{
			if (_shutDownInProgress)
				return;

			_shutDownInProgress = true;

			try
			{
				if (force)
				{
					_server?.DisposeSubSystems();
					Process.GetCurrentProcess().Kill();
					return;
				}

				// TODO: reimplement this
				//if (MessageBox.Show("Вы действительно хотите отключить систему?", "Завершение работы",
				//		MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.No)
				//	return;

				_serverLogger?.LogInfo("Disposing the application...");

				_mainWindowVm?.Dispose();
				_server?.Dispose();
				_clientLogger?.Dispose();
				_integrationLogger?.Dispose();
				_deviceLogger?.Dispose();
				_serverLogger?.Dispose();
				_httpClient?.Dispose();

				_serverLogger?.LogInfo("Application stopped");

				if (!shutPcDown)
					return;

				_serverLogger?.LogInfo("Shutting down the system...");
				_server?.ShutPcDown();
			}
			catch (Exception ex)
			{
				_serverLogger.LogException("Failed to close the application", ex);
			}
			finally
			{
				_shutDownInProgress = false;
			}
		}

		private async Task InitializeServerAsync()
		{
			var logFatalMessage = "";
			var displayFatalMessage = "";

			try
			{
				_server = new ServerComponentsHandler(_serverLogger, _httpClient);

				_serverLogger.LogInfo("Reading settings...");

				logFatalMessage = "FATAL: Failed to initialize application settings!";
				displayFatalMessage = "Не удалось инициализировать настройки приложения";

				await _server.InitializeSettingsAsync();
				_server.ApplicationSettingsChanged += OnApplicationSettingsUpdated;

				_serverLogger.LogInfo("Settings - ok");
				_serverLogger.LogInfo("Initializing IO devices...");

				logFatalMessage = "FATAL: Failed to initialize IO devices!";
				displayFatalMessage = "Не удалось инициализировать внешние устройства";

				_server.InitializeIoDevicesAsync(_deviceLogger);

				_serverLogger.LogInfo("IO devices- ok");
				_serverLogger.LogInfo("Initializing calculation systems...");

				logFatalMessage = "FATAL: Failed to initialize calculation systems!";
				displayFatalMessage = "Не удалось инициализировать логику вычисления";

				_server.InitializeCalculationSystems();

				_serverLogger.LogInfo("Calculation systems - ok");
				_serverLogger.LogInfo("Initializing interration systems...");

				logFatalMessage = "FATAL: Failed to initialize integration systems!";
				displayFatalMessage = "Не удалось инициализировать внешние интеграции";

				await _server.InitializeIntegrationsAsync(_integrationLogger);

				_serverLogger.LogInfo("Integration systems - ok");

			}
			catch (Exception ex)
			{
				_serverLogger.LogException(logFatalMessage, ex);
				_fatalErrorMessages.Add(displayFatalMessage);
				throw;
			}
		}

		private void InitializeClient()
		{
			try
			{
				_clientLogger.LogInfo("Initializing GUI handlers...");

				_mainWindowVm = new MainWindowVm(_clientLogger, _httpClient, _server);
				_mainWindowVm.ShutDownRequested += ShutDown;
				_mainWindowVm.CalculationStartRequested += OnCalculationStartRequested;
				_mainWindowVm.InitializeSubViewModels();

				_mainWindow = new MainWindow { DataContext = _mainWindowVm };

				_clientLogger.LogInfo("GUI handlers - ok");
			}
			catch (Exception ex)
			{
				_clientLogger.LogException("FATAL: Failed to initialize GUI!", ex);

				var message = "Не удалось инициализировать основные программные компоненты системы";
				_fatalErrorMessages.Add(message);
				throw;
			}
		}

		private void DisplayFatalErrorsAndCloseApplication()
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

		private void OnUnhandledException(object sender, UnhandledExceptionEventArgs e)
		{
			_clientLogger.LogException("Unhandled exception in application domain occured, app terminates...",
				(Exception)e.ExceptionObject);

			DisplayFatalErrorsAndCloseApplication();
		}

		private void OnApplicationSettingsUpdated(ApplicationSettings settings)
		{
			_mainWindowVm.UpdateApplicationSettings(settings);
		}

		private void OnCalculationStartRequested()
		{
			_server.Calculator.StartCalculation(null);
		}
	}
}
