using System;
using System.Net;
using System.Threading.Tasks;
using Primitives.Logging;

namespace ExtIntegration
{
	public class HttpRequestHandler : IDisposable
	{
		public event Action<HttpListenerContext> RequestReceived;

		private readonly ILogger _logger;
		private readonly HttpListener _listener;
		private readonly string _fullAddress;

		private bool _running;

		public HttpRequestHandler(ILogger logger, string address, string port)
		{
			_logger = logger;
			_fullAddress = $"http://{address}:{port}";
			logger.LogInfo($"Starting http listener for {_fullAddress}...");

			_listener = new HttpListener();
			_listener.Prefixes.Add(_fullAddress);

			_listener.Start();
			_running = true;

			Task.Run(() => ListenToRequests());
		}

		public void Dispose()
		{
			_logger.LogInfo($"Disposing http listener for {_fullAddress}...");
			_running = false;
			_listener.Close();
		}

		private void ListenToRequests()
		{
			while (_running)
			{
				try
				{
					Console.WriteLine("Listening...");
					// Note: The GetContext method blocks while waiting for a request. 
					var listenerContext = _listener.GetContext();
					RequestReceived?.Invoke(listenerContext);
				}
				catch (Exception ex)
				{
					_logger.LogException($"Failed to process incoming http request for {_fullAddress}", ex);
				}
			}
		}
	}
}