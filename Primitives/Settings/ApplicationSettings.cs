using System.Runtime.Serialization;

namespace Primitives.Settings
{
	public class ApplicationSettings
    {
	    public IoSettings IoSettings { get; set; }

		public AlgorithmSettings AlgorithmSettings { get; set; }

		public IntegrationSettings IntegrationSettings { get; set; }

	    public ApplicationSettings(IoSettings ioSettings, AlgorithmSettings algorithmSettings, IntegrationSettings integrationSettings)
	    {
		    IoSettings = ioSettings;
		    AlgorithmSettings = algorithmSettings;
		    IntegrationSettings = integrationSettings;
	    }

	    public static ApplicationSettings GetDefaultSettings()
	    {
		    return new ApplicationSettings(IoSettings.GetDefaultSettings(), AlgorithmSettings.GetDefaultSettings(), 
			    IntegrationSettings.GetDefaultSettings());
	    }

	    public override string ToString()
	    {
		    return $"{IoSettings}; {AlgorithmSettings}; {IntegrationSettings}";
	    }

		[OnDeserialized]
	    private void OnDeserialized(StreamingContext context)
		{
			if (IoSettings == null)
				IoSettings = IoSettings.GetDefaultSettings();

			if (AlgorithmSettings == null)
				AlgorithmSettings = AlgorithmSettings.GetDefaultSettings();

			if (IntegrationSettings == null)
				IntegrationSettings = IntegrationSettings.GetDefaultSettings();
	    }
    }
}