using System.Runtime.Serialization;

namespace Primitives.Settings.Integration
{
	public class HttpRequestSettings
	{
		public bool EnableRequests { get; set; }

		public string[] DestinationIps { get; set; }

		public int Port { get; set; }

		public string Url { get; set; }

		public string Login { get; set; }

		public string Password { get; set; }

		public HttpRequestSettings(bool enableRequests, string[] destinationIps, int port, string url, string login, string password)
		{
			EnableRequests = enableRequests;
			DestinationIps = destinationIps;
			Port = port;
			Url = url;
			Login = login;
			Password = password;
		}

		public static HttpRequestSettings GetDefaultSettings()
		{
			var defaultAddresses = new[] {"localhost"};

			return new HttpRequestSettings(false, defaultAddresses, 8888, "", "", "");
		}

		[OnDeserialized]
		public void OnDeserialized(StreamingContext context)
		{
			if (DestinationIps == null)
				DestinationIps = new[] {"localhost"};

			if (Login == null)
				Login = "";

			if (Password == null)
				Password = "";
		}
	}
}