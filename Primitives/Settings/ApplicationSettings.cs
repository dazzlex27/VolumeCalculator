using System.Runtime.Serialization;

namespace Primitives.Settings
{
	public class ApplicationSettings
    {
	    public IoSettings IoSettings { get; set; }

		public AlgorithmSettings AlgorithmSettings { get; set; }

		public WebRequestSettings WebRequestSettings { get; set; }

		public SqlRequestSettings SqlRequestSettings { get; set; }

		public ApplicationSettings(IoSettings ioSettings, AlgorithmSettings algorithmSettings,
			WebRequestSettings webRequestSettings, SqlRequestSettings sqlRequestSettings)
		{
			IoSettings = ioSettings;
			AlgorithmSettings = algorithmSettings;
		    WebRequestSettings = webRequestSettings;
		    SqlRequestSettings = sqlRequestSettings;
	    }

	    public static ApplicationSettings GetDefaultSettings()
	    {
		    return new ApplicationSettings(IoSettings.GetDefaultSettings(), AlgorithmSettings.GetDefaultSettings(), 
			    WebRequestSettings.GetDefaultSettings(), SqlRequestSettings.GetDefaultSettings());
	    }

	    public override string ToString()
	    {
		    return $"{IoSettings}; {AlgorithmSettings}; {WebRequestSettings}; {SqlRequestSettings}";
	    }

		[OnDeserializing]
	    private void OnDeserialize(StreamingContext context)
		{
			if (IoSettings == null)
				IoSettings = IoSettings.GetDefaultSettings();

			if (AlgorithmSettings == null)
				AlgorithmSettings = AlgorithmSettings.GetDefaultSettings();

			if (WebRequestSettings == null)
				WebRequestSettings = WebRequestSettings.GetDefaultSettings();

			if (SqlRequestSettings == null)
				SqlRequestSettings = SqlRequestSettings.GetDefaultSettings();
	    }
    }
}