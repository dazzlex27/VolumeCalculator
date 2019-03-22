namespace Primitives.Settings.Integration
{
	public class HttpApiSettings
	{
		public bool EnableRequests { get; set; }

		public string Address { get; set; }

		public int Port { get; set; }

		public HttpApiSettings(bool enableRequests, string address, int port)
		{
			EnableRequests = enableRequests;
			Address = address;
			Port = port;
		}

		public static HttpApiSettings GetDefaultSettings()
		{
			return new HttpApiSettings(true, "+", 8080);
		}
	}
}