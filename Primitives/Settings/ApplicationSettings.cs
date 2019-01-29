using System.Reflection;
using System.Runtime.Serialization;

namespace Primitives.Settings
{
	[Obfuscation(Exclude = true, Feature = "rename")]
	public class ApplicationSettings
    {
	    [Obfuscation]
	    public IoSettings IoSettings { get; set; }

	    [Obfuscation]
		public AlgorithmSettings AlgorithmSettings { get; set; }

		[Obfuscation]
		public WebRequestSettings WebRequestSettings { get; set; }

	    [Obfuscation]
		public SqlRequestSettings SqlRequestSettings { get; set; }

		public ApplicationSettings(IoSettings ioSettings, AlgorithmSettings algorithmSettings,
			WebRequestSettings webRequestSettings, SqlRequestSettings sqlRequestSettings)
		{
			IoSettings = ioSettings;
			AlgorithmSettings = algorithmSettings;
		    WebRequestSettings = webRequestSettings;
		    SqlRequestSettings = sqlRequestSettings;
	    }

	    [Obfuscation(Exclude = true)]
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