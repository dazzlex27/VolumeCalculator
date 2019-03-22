using System.Runtime.Serialization;

namespace Primitives.Settings.Integration
{
	public class IntegrationSettings
	{
		public WebClientHandlerSettings WebClientHandlerSettings { get; set; }

		public HttpApiSettings HttpApiSettings { get; set; }

		public HttpRequestSettings HttpRequestSettings { get; set; }

		public SqlRequestSettings SqlRequestSettings { get; set; }

		public FtpRequestSettings FtpRequestSettings { get; set; }

		public IntegrationSettings(WebClientHandlerSettings webSocketHandlerSettings, HttpApiSettings httpApiSettings,
			HttpRequestSettings httpRequestSettings, SqlRequestSettings sqlRequestSettings, FtpRequestSettings ftpRequestSettings)
		{
			WebClientHandlerSettings = webSocketHandlerSettings;
			HttpApiSettings = httpApiSettings;
			HttpRequestSettings = httpRequestSettings;
			SqlRequestSettings = sqlRequestSettings;
			FtpRequestSettings = ftpRequestSettings;
		}

		public static IntegrationSettings GetDefaultSettings()
		{
			var webClientHandlerSettings = WebClientHandlerSettings.GetDefaultSettings();
			var httpApiSettings = HttpApiSettings.GetDefaultSettings();
			var httpRequestSettings = HttpRequestSettings.GetDefaultSettings();
			var sqlRequestSettings = SqlRequestSettings.GetDefaultSettings();
			var ftpRequestSettings = FtpRequestSettings.GetDefaultSettings();

			return new IntegrationSettings(webClientHandlerSettings, httpApiSettings, httpRequestSettings,
				sqlRequestSettings, ftpRequestSettings);
		}

		[OnDeserialized]
		private void OnDeserialized(StreamingContext context)
		{
			if (WebClientHandlerSettings == null)
				WebClientHandlerSettings = WebClientHandlerSettings.GetDefaultSettings();

			if (HttpApiSettings == null)
				HttpApiSettings = HttpApiSettings.GetDefaultSettings();

			if (SqlRequestSettings == null)
				SqlRequestSettings = SqlRequestSettings.GetDefaultSettings();

			if (HttpRequestSettings == null)
				HttpRequestSettings = HttpRequestSettings.GetDefaultSettings();

			if (FtpRequestSettings == null)
				FtpRequestSettings = FtpRequestSettings.GetDefaultSettings();
		}
	}
}