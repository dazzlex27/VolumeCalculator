using ExtIntegration;

namespace Primitives
{
	public class WebRequestSettings
	{
		public bool EnableRequests { get; set; }

		public string Address { get; set; }

		public int Port { get; set; }

		public bool SendGetRequests { get; set; }

		public bool SendPostRequests { get; set; }

		public bool SendWeight { get; set; }

		public bool SendLength { get; set; }

		public bool SendWidth { get; set; }

		public bool SendHeight { get; set; }

		public bool SendBarcode { get; set; }

		public bool SendCount { get; set; }

		public bool SendComment { get; set; }

		public WebRequestSettings(bool enableRequests, string address, int port, bool sendGetRequests = true, bool sendPostRequests = true,
			bool sendWeight = true, bool sendLength = true, bool sendWidth = true, bool sendHeight = true,
			bool sendBarcode = true, bool sendCount = true, bool sendComment = true)
		{
			EnableRequests = enableRequests;
			Address = address;
			Port = port;
			SendGetRequests = sendGetRequests;
			SendPostRequests = sendPostRequests;
			SendWeight = sendWeight;
			SendLength = sendLength;
			SendWidth = sendWidth;
			SendHeight = sendHeight;
			SendBarcode = sendBarcode;
			SendCount = sendCount;
			SendComment = sendComment;
		}

		public static WebRequestSettings GetDefaultSettings()
		{
			return new WebRequestSettings(false, "localhost", 8080);
		}
	}
}