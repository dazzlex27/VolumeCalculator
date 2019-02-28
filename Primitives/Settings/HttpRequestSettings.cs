namespace Primitives.Settings
{
	public class HttpRequestSettings
	{
		public bool EnableRequests { get; set; }

		public string Address { get; set; }

		public int Port { get; set; }

		public string Url { get; set; }

		public HttpRequestSettings(bool enableRequests, string address, int port, string url)
		{
			EnableRequests = enableRequests;
			Address = address;
			Port = port;
			Url = url;
		}

		public static HttpRequestSettings GetDefaultSettings()
		{
			return new HttpRequestSettings(false, "localhost", 8888, "");
		}
	}
}