using System.Net;

namespace ExtIntegration.RequestHandlers
{
	public class HttpRequestData
	{
		public HttpListenerContext Context { get; }

		public bool SendPhoto { get; }

		public HttpRequestData(HttpListenerContext context, bool sendPhoto)
		{
			Context = context;
			SendPhoto = sendPhoto;
		}
	}
}