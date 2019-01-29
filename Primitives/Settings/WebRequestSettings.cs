namespace Primitives.Settings
{
	public class WebRequestSettings
	{
		public bool EnableRequests { get; set; }

		public string Address { get; set; }

		public int Port { get; set; }

		public WebRequestSettings(bool enableRequests, string address, int port)
		{
			EnableRequests = enableRequests;
			Address = address;
			Port = port;
		}

		public static WebRequestSettings GetDefaultSettings()
		{
			return new WebRequestSettings(false, "localhost", 8080);
		}
	}
}