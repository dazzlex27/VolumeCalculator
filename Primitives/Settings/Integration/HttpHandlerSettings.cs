namespace Primitives.Settings.Integration
{
	public class HttpHandlerSettings
	{
		public bool EnableRequests { get; set; }

		public string Address { get; set; }

		public int Port { get; set; }

		public HttpHandlerSettings(bool enableRequests, string address, int port)
		{
			EnableRequests = enableRequests;
			Address = address;
			Port = port;
		}

		public static HttpHandlerSettings GetDefaultSettings()
		{
			return new HttpHandlerSettings(true, "+", 8080);
		}
	}
}