namespace Primitives.Settings.Integration
{
	public class WebSocketHandlerSettings
	{
		public bool EnableRequests { get; set; }

		public string Address { get; set; }

		public int Port { get; set; }

		public WebSocketHandlerSettings(bool enableRequests, string address, int port)
		{
			EnableRequests = enableRequests;
			Address = address;
			Port = port;
		}

		public static WebSocketHandlerSettings GetDefaultSettings()
		{
			return new WebSocketHandlerSettings(true, "0.0.0.0", 8081);
		}
	}
}
