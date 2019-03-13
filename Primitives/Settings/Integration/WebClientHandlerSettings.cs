namespace Primitives.Settings.Integration
{
	public class WebClientHandlerSettings
	{
		public bool EnableRequests { get; set; }

		public string Address { get; set; }

		public int Port { get; set; }

		public WebClientHandlerSettings(bool enableRequests, string address, int port)
		{
			EnableRequests = enableRequests;
			Address = address;
			Port = port;
		}

		public static WebClientHandlerSettings GetDefaultSettings()
		{
			return new WebClientHandlerSettings(true, "0.0.0.0", 8081);
		}
	}
}
