using System.Net;

namespace ExtIntegration.RequestContexts
{
	public class HttpRequestContext : IRequestContext
	{
		public HttpListenerContext Context { get; }

		public HttpRequestContext(HttpListenerContext context)
		{
			Context = context;
		}
	}
}