using System;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;
using Primitives;
using Primitives.Logging;
using Primitives.Settings;

namespace ExtIntegration.RequestHandlers
{
	public class HttpRequestHandler
	{
		public event Action<HttpListenerContext> CalculationStartRequested;
		public event Action CalculationStartRequestTimedOut;

		private const int RequestTimeoutMs = 15000;
		private const int ThreadIdleTimeSpan = 500;

		private readonly System.Timers.Timer _requesthandlingTimeoutTimer;
		private readonly ILogger _logger;
		private readonly HttpListener _listener;
		private readonly string _address;

		private bool _running;
		private bool _sessionInProgress;

		public HttpRequestHandler(ILogger logger, HttpHandlerSettings settings)
		{
			_logger = logger;
			_address = $"http://{settings.Address}:{settings.Port}/";

			_logger.LogInfo($"Creating http listener for {_address} ...");

			_listener = new HttpListener();
			_listener.Prefixes.Add(_address);
			_running = true;

			_requesthandlingTimeoutTimer = new System.Timers.Timer(RequestTimeoutMs) { AutoReset = false };
			_requesthandlingTimeoutTimer.Elapsed += OnTimeoutTimerElapsed;

			Task.Run(() =>
			{
				try
				{
					ListenToRequests();
				}
				catch (Exception ex)
				{
					_logger.LogException("Http listening failed", ex);
				}
			});
		}

		public void Dispose()
		{
			_logger.LogInfo($"Disposing http listener for {_address} ...");
			_running = false;
		}

		public void SendResponse(HttpListenerContext context, CalculationResult result)
		{
			try
			{
				if (!_sessionInProgress)
					_logger.LogError($"Response generation call for {_address} came after the session timeout");

				_logger.LogInfo($"Generating HTTP response for {_address}...");
				_requesthandlingTimeoutTimer.Stop();
				var responseString = RequestUtils.GenerateXmlResponseText(result, result.Status);
				_logger.LogInfo($"Response text is the following: {Environment.NewLine}{responseString}");

				var response = context.Response;
				var buffer = System.Text.Encoding.UTF8.GetBytes(responseString);

				response.ContentLength64 = buffer.Length;
				var output = response.OutputStream;
				output.Write(buffer, 0, buffer.Length);

				output.Close();

				_logger.LogInfo($"Sent HTTP response for {_address}");
			}
			catch (Exception ex)
			{
				_logger.LogException($"Failed to send HTTP response to {_address}", ex);
			}
			finally
			{
				_sessionInProgress = false;
			}
		}

		public void Reset(HttpListenerContext context, string text)
		{
			try
			{
				_logger.LogInfo($"Sending an HTTP reset message to {_address}, the message is \"{text}\"");
				var response = context.Response;
				var buffer = System.Text.Encoding.UTF8.GetBytes(text);

				response.ContentLength64 = buffer.Length;
				var output = response.OutputStream;
				output.Write(buffer, 0, buffer.Length);

				output.Close();
			}
			catch (Exception ex)
			{
				_logger.LogException($"Failed to send HTTP reset to {_address}", ex);
			}
			finally
			{
				_sessionInProgress = false;
			}
		}

		private void ListenToRequests()
		{
			_listener.Start();

			while (_running)
			{
				if (_sessionInProgress)
					Thread.Sleep(ThreadIdleTimeSpan);

				_logger.LogInfo($"Listening for HTTP requests from {_address}...");
				var context = _listener.GetContext();
				var request = context.Request;
				var url = request.RawUrl;
				var command = url.Contains("?")
					? url.Substring(1, url.IndexOf("?", StringComparison.Ordinal))
					: url.Substring(1);
				var method = request.HttpMethod;
				_logger.LogInfo($"HTTP request accepted from {_address} containing {url}, the method is {method}");

				if (command != "calculate")
					continue;

				_logger.LogInfo($"Received a calculate command from {_address}...");
				RaiseCalculationStartRequestedEvent(context);
			}

			_listener.Stop();
		}

		private void RaiseCalculationStartRequestedEvent(HttpListenerContext context)
		{
			CalculationStartRequested?.Invoke(context);
			_sessionInProgress = true;
			_requesthandlingTimeoutTimer.Start();
		}

		private void OnTimeoutTimerElapsed(object sender, ElapsedEventArgs e)
		{
			_logger.LogError($"HTTP request handling for {_address} timed out, aborting request...");
			CalculationStartRequestTimedOut?.Invoke();
			_sessionInProgress = false;
		}
	}
}