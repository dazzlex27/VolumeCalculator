using System.Diagnostics;
using System.Windows.Input;
using Primitives;
using Primitives.Settings;

namespace VCConfigurator
{
	internal class MainWindowVm : BaseViewModel
	{
		private readonly ApplicationSettings _settings;

		private DeviceSettingsVm _deviceSetingsVm;
		private IntegrationSettingsVm _integrationSettingsVm;

		public DeviceSettingsVm DeviceSettingsVm
		{
			get => _deviceSetingsVm;
			set
			{
				if (_deviceSetingsVm == value)
					return;

				_deviceSetingsVm = value;
				OnPropertyChanged();
			}
		}

		public IntegrationSettingsVm IntegrationSettingsVm
		{
			get => _integrationSettingsVm;
			set
			{
				if (_integrationSettingsVm == value)
					return;

				_integrationSettingsVm = value;
				OnPropertyChanged();
			}
		}

		public ICommand ApplySettingsAndCloseCommand { get; set; }

		public ICommand ApplySettingsCommand { get; set; }

		public MainWindowVm()
		{
			DeviceSettingsVm = new DeviceSettingsVm();
			IntegrationSettingsVm = new IntegrationSettingsVm();

			_settings = ReadSettingsFromFile();
			FillValuesFromSettings(_settings);

			ApplySettingsAndCloseCommand = new CommandHandler(() => ApplySettings(true), true);
			ApplySettingsCommand = new CommandHandler(() => ApplySettings(false), true);
		}

		private static ApplicationSettings ReadSettingsFromFile()
		{
			ApplicationSettings settings;

			try
			{
				var settingsFromFile = IoUtils.DeserializeSettings();
				if (settingsFromFile != null)
					return settingsFromFile;
			}
			finally
			{
				settings = ApplicationSettings.GetDefaultSettings();
			}

			return settings;
		}

		private void FillValuesFromSettings(ApplicationSettings settings)
		{
			_deviceSetingsVm.FillValuesFromSettings(settings.IoSettings);
			_integrationSettingsVm.FillValuesFromSettings(settings.IntegrationSettings);
		}

		private void ApplySettings(bool closeApplication)
		{
			_deviceSetingsVm.FillSettingsFromValues(_settings.IoSettings);
			_integrationSettingsVm.FillSettingsFromValues(_settings.IntegrationSettings);

			IoUtils.SerializeSettings(_settings);

			if (closeApplication)
				Process.GetCurrentProcess().Kill();
		}
	}
}