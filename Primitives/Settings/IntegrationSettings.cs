using System.Runtime.Serialization;

namespace Primitives.Settings
{
	public class IntegrationSettings
	{
		public WebSocketHandlerSettings WebSocketHandlerSettings { get; set; }

		public HttpHandlerSettings HttpHandlerSettings { get; set; }

		public HttpRequestSettings HttpRequestSettings { get; set; }

		public SqlRequestSettings SqlRequestSettings { get; set; }

		public IntegrationSettings(WebSocketHandlerSettings webSocketHandlerSettings, HttpHandlerSettings httpHandlerSettings,
			HttpRequestSettings httpRequestSettings, SqlRequestSettings sqlRequestSettings)
		{
			WebSocketHandlerSettings = webSocketHandlerSettings;
			HttpHandlerSettings = httpHandlerSettings;
			HttpRequestSettings = httpRequestSettings;
			SqlRequestSettings = sqlRequestSettings;
		}

		public static IntegrationSettings GetDefaultSettings()
		{
			var webSocketHandlerSettings = WebSocketHandlerSettings.GetDefaultSettings();
			var httpHandlerSettings = HttpHandlerSettings.GetDefaultSettings();
			var httpRequestSettings = HttpRequestSettings.GetDefaultSettings();
			var sqlRequestSettings = SqlRequestSettings.GetDefaultSettings();

			return new IntegrationSettings(webSocketHandlerSettings, httpHandlerSettings, httpRequestSettings, sqlRequestSettings);
		}

		[OnDeserialized]
		private void OnDeserialized(StreamingContext context)
		{
			if (WebSocketHandlerSettings == null)
				WebSocketHandlerSettings = WebSocketHandlerSettings.GetDefaultSettings();

			if (HttpRequestSettings == null)
				HttpHandlerSettings = HttpHandlerSettings.GetDefaultSettings();

			if (SqlRequestSettings == null)
				SqlRequestSettings = SqlRequestSettings.GetDefaultSettings();

			if (HttpRequestSettings == null)
				HttpRequestSettings = HttpRequestSettings.GetDefaultSettings();
		}
	}
}
