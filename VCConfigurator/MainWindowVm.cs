using System;
using System.Diagnostics;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using CommonUtils.Logging;
using GuiCommon;
using Primitives;
using Primitives.Logging;
using Primitives.Settings;
using ProcessingUtils;

namespace VCConfigurator
{
	internal class MainWindowVm : BaseViewModel, IDisposable
	{
		private readonly ILogger _logger;
		private readonly ApplicationSettings _settings;

		private DeviceSettingsVm _deviceSettingsVm;
		private IntegrationSettingsVm _integrationSettingsVm;

		public DeviceSettingsVm DeviceSettingsVm
		{
			get => _deviceSettingsVm;
			set => SetField(ref _deviceSettingsVm, value, nameof(DeviceSettingsVm));
		}

		public IntegrationSettingsVm IntegrationSettingsVm
		{
			get => _integrationSettingsVm;
			set => SetField(ref _integrationSettingsVm, value, nameof(IntegrationSettingsVm));
		}

		public ICommand ApplySettingsAndRunVCalcCommand { get; set; }

		public ICommand ApplySettingsCommand { get; set; }

		public MainWindowVm()
		{
			_logger = new TxtLogger("VCConfigurator", "configurator");
			_logger.LogInfo($"Starting up Configurator app...");
			AppDomain.CurrentDomain.UnhandledException += OnUnhandledException;

			DeviceSettingsVm = new DeviceSettingsVm();
			IntegrationSettingsVm = new IntegrationSettingsVm();

			_settings = Task.Run(async () => await ReadSettingsFromFileAsync()).Result;
			FillValuesFromSettings(_settings);

			ApplySettingsAndRunVCalcCommand = new CommandHandler(async () => await ApplySettingsAndClose(true), true);
			ApplySettingsCommand = new CommandHandler(async () => await ApplySettingsAndClose(false), true);
		}

		public void Dispose()
		{
			_logger?.Dispose();
		}

		private async Task<ApplicationSettings> ReadSettingsFromFileAsync()
		{
			ApplicationSettings settings;

			try
			{
				var settingsFromFile =
					await IoUtils.DeserializeSettingsFromFileAsync<ApplicationSettings>(GlobalConstants.ConfigFileName);
				if (settingsFromFile != null)
					return settingsFromFile;
			}
			catch (Exception ex)
			{
				_logger.LogException("Failed to read settings from file", ex);
			}
			finally
			{
				settings = ApplicationSettings.GetDefaultSettings();
			}

			return settings;
		}

		private void FillValuesFromSettings(ApplicationSettings settings)
		{
			_deviceSettingsVm.FillValuesFromSettings(settings.IoSettings);
			_integrationSettingsVm.FillValuesFromSettings(settings.IntegrationSettings);
		}

		private async Task ApplySettingsAndClose(bool startClient)
		{
			try
			{
				_logger.LogInfo("Applying settings...");

				_deviceSettingsVm.FillSettingsFromValues(_settings.IoSettings);
				_integrationSettingsVm.FillSettingsFromValues(_settings.IntegrationSettings);

				await IoUtils.SerializeSettingsToFileAsync(_settings, GlobalConstants.ConfigFileName);

				if (startClient)
					IoUtils.StartProcess("VCClient.exe", true);

				Process.GetCurrentProcess().Kill();
			}
			catch (Exception ex)
			{
				_logger.LogException("Failed to apply settings", ex);
			}
		}

		private void OnUnhandledException(object sender, UnhandledExceptionEventArgs e)
		{
			_logger.LogException("Unhandled exception in application domain occured, app terminates...",
				(Exception)e.ExceptionObject);

			DisplayFatalErrorsAndCloseApplication();
		}

		private void DisplayFatalErrorsAndCloseApplication()
		{
			var builder = new StringBuilder();
			builder.AppendLine("Произошли критические ошибки");
			builder.AppendLine("Приложение будет закрыто, информация записана в журнал");

			AutoClosingMessageBox.Show(builder.ToString(), "Аварийное завершение", 5000);
		}
	}
}
