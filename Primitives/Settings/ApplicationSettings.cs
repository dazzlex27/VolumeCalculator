using System.Runtime.Serialization;

namespace Primitives.Settings
{
	public class ApplicationSettings
    {
	    public IoSettings IoSettings { get; set; }

		public AlgorithmSettings AlgorithmSettings { get; set; }

		public HttpRequestSettings HttpRequestSettings { get; set; }

		public SqlRequestSettings SqlRequestSettings { get; set; }

		public ApplicationSettings(IoSettings ioSettings, AlgorithmSettings algorithmSettings,
			HttpRequestSettings httpRequestSettings, SqlRequestSettings sqlRequestSettings)
		{
			IoSettings = ioSettings;
			AlgorithmSettings = algorithmSettings;
		    HttpRequestSettings = httpRequestSettings;
		    SqlRequestSettings = sqlRequestSettings;
	    }

	    public static ApplicationSettings GetDefaultSettings()
	    {
		    return new ApplicationSettings(IoSettings.GetDefaultSettings(), AlgorithmSettings.GetDefaultSettings(), 
			    HttpRequestSettings.GetDefaultSettings(), SqlRequestSettings.GetDefaultSettings());
	    }

	    public override string ToString()
	    {
		    return $"{IoSettings}; {AlgorithmSettings}; {HttpRequestSettings}; {SqlRequestSettings}";
	    }

		[OnDeserializing]
	    private void OnDeserialize(StreamingContext context)
		{
			if (IoSettings == null)
				IoSettings = IoSettings.GetDefaultSettings();

			if (AlgorithmSettings == null)
				AlgorithmSettings = AlgorithmSettings.GetDefaultSettings();

			if (HttpRequestSettings == null)
				HttpRequestSettings = HttpRequestSettings.GetDefaultSettings();

			if (SqlRequestSettings == null)
				SqlRequestSettings = SqlRequestSettings.GetDefaultSettings();
	    }
    }
}