using System.Runtime.Serialization;

namespace Primitives.Settings.Integration
{
	public class HttpApiSettings
	{
		public bool EnableRequests { get; set; }

		public string Address { get; set; }

		public int Port { get; set; }

		public string Login { get; set; }

		public string Password { get; set; }

		public HttpApiSettings(bool enableRequests, string address, int port, string login, string password)
		{
			EnableRequests = enableRequests;
			Address = address;
			Port = port;
			Login = login;
			Password = password;
		}

		public static HttpApiSettings GetDefaultSettings()
		{
			return new HttpApiSettings(true, "+", 8080, "", "");
		}

		[OnDeserialized]
		public void OnDeserialized(StreamingContext context)
		{
			if (Login == null)
				Login = "";

			if (Password == null)
				Password = "";
		}
	}
}