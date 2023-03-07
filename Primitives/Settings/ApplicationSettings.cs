using System.Runtime.Serialization;
using Primitives.Settings.Integration;

namespace Primitives.Settings
{
	public class ApplicationSettings
	{
		public GeneralSettings GeneralSettings { get; set; }

		public IoSettings IoSettings { get; set; }

		public AlgorithmSettings AlgorithmSettings { get; set; }

		public IntegrationSettings IntegrationSettings { get; set; }

		public ApplicationSettings(GeneralSettings generalSettings, IoSettings ioSettings,
			AlgorithmSettings algorithmSettings, IntegrationSettings integrationSettings)
		{
			GeneralSettings = generalSettings;
			IoSettings = ioSettings;
			AlgorithmSettings = algorithmSettings;
			IntegrationSettings = integrationSettings;
		}

		public static ApplicationSettings GetDefaultSettings()
		{
			return new ApplicationSettings(GeneralSettings.GetDefaultSettings(), IoSettings.GetDefaultSettings(),
				AlgorithmSettings.GetDefaultSettings(), IntegrationSettings.GetDefaultSettings());
		}

		public override string ToString()
		{
			return $"{IoSettings}; {AlgorithmSettings}; {IntegrationSettings}";
		}

		[OnDeserialized]
		private void OnDeserialized(StreamingContext context)
		{
			if (GeneralSettings == null)
				GeneralSettings = GeneralSettings.GetDefaultSettings();

			if (IoSettings == null)
				IoSettings = IoSettings.GetDefaultSettings();

			if (AlgorithmSettings == null)
				AlgorithmSettings = AlgorithmSettings.GetDefaultSettings();

			if (IntegrationSettings == null)
				IntegrationSettings = IntegrationSettings.GetDefaultSettings();
		}
	}
}
