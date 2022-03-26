using System;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;
using Primitives;
using Primitives.Logging;
using Primitives.Settings.Integration;

namespace ExtIntegration.RequestHandlers
{
	public class HttpRequestHandler : IDisposable
	{
		public event Action<HttpRequestData> CalculationStartRequested;
		public event Action CalculationStartRequestTimedOut;

		private const int RequestTimeoutMs = 15000;
		private const int ThreadIdleTimeSpan = 500;

		private readonly System.Timers.Timer _requestHandlingTimeoutTimer;
		private readonly ILogger _logger;
		private readonly HttpListener _listener;
		private readonly string _address;
		private readonly string _login;
		private readonly string _password;

		private bool _running;
		private bool _sessionInProgress;

		public HttpRequestHandler(ILogger logger, HttpApiSettings settings)
		{
			_logger = logger;
			_address = $"http://{settings.Address}:{settings.Port}/";

			_login = settings.Login;
			_password = settings.Password;

			_logger.LogInfo($"Creating http listener for {_address} ...");

			_listener = new HttpListener();
			_listener.Prefixes.Add(_address);
			_running = true;

			_requestHandlingTimeoutTimer = new System.Timers.Timer(RequestTimeoutMs) { AutoReset = false };
			_requestHandlingTimeoutTimer.Elapsed += OnTimeoutTimerElapsed;

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

		public void SendResponse(HttpRequestData data, CalculationResultData resultData)
		{
			try
			{
				if (!_sessionInProgress)
					_logger.LogError($"Response generation call for {_address} came after the session timeout");

				_requestHandlingTimeoutTimer.Stop();

				_logger.LogInfo($"Generating HTTP response for {_address}...");
				var responseString = RequestUtils.GenerateXmlResponseText(resultData, data.SendPhoto);
				_logger.LogInfo($"Response text is the following: {Environment.NewLine}{responseString}");

				var response = data.Context.Response;
				var buffer = Encoding.UTF8.GetBytes(responseString);

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

		private void SendPingResponse(HttpRequestData data)
		{
			try
			{
				const string responseString = "pong";
				_logger.LogInfo($"Response text is the following: {Environment.NewLine}{responseString}");

				var response = data.Context.Response;
				var buffer = Encoding.UTF8.GetBytes(responseString);

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

		public void Reset(HttpRequestData data, string text)
		{
			try
			{
				_logger.LogInfo($"Sending an HTTP reset message to {_address}, the message is \"{text}\"");
				var response = data.Context.Response;
				var buffer = Encoding.UTF8.GetBytes(text);

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

				_logger.LogInfo($"Awaiting HTTP requests from {_address}...");
				var context = _listener.GetContext();
				var request = context.Request;
				var credentialsAreOk = CheckCredentials(request);
				if (!credentialsAreOk)
				{
					_logger.LogError($"Credentials from {_address} were incorrect, will not process the request");
					continue;
				}

				var url = request.RawUrl;
				var command = url.Contains("?")
					? url.Substring(1, url.IndexOf("?", StringComparison.Ordinal))
					: url.Substring(1);
				var method = request.HttpMethod;
				_logger.LogInfo($"HTTP request accepted from {_address} containing {url}, the method is {method}");

				_logger.LogInfo($"Received a \"{command}\" command from {_address}...");

				switch (command)
				{
					case "ping":
						SendPingResponse(new HttpRequestData(context, false));
						break;
					case "calculate":
						RaiseCalculationStartRequestedEvent(context, false);
						break;
					case "calculate_ph":
						RaiseCalculationStartRequestedEvent(context, true);
						break;
					default:
						_logger.LogInfo($"Received an unknown command \"{command}\"");
						break;
				}
			}

			_listener.Stop();
		}

		private void RaiseCalculationStartRequestedEvent(HttpListenerContext context, bool sendPhoto)
		{
			var requestData = new HttpRequestData(context, sendPhoto);

			CalculationStartRequested?.Invoke(requestData);
			_sessionInProgress = true;
			_requestHandlingTimeoutTimer.Start();
		}

		private void OnTimeoutTimerElapsed(object sender, ElapsedEventArgs e)
		{
			_logger.LogError($"HTTP request handling for {_address} timed out, aborting request...");
			CalculationStartRequestTimedOut?.Invoke();
			_sessionInProgress = false;
		}

		private bool CheckCredentials(HttpListenerRequest request)
		{
			if (string.IsNullOrEmpty(_login))
				return true;

			var authHeader = request.Headers["Authorization"];
			var headerCorrect = authHeader != null && authHeader.StartsWith("Basic");
			if (!headerCorrect)
				return false;

			var encodedUsernamePassword = authHeader.Substring("Basic ".Length).Trim();
			var encoding = Encoding.GetEncoding("iso-8859-1");
			var usernamePassword = encoding.GetString(Convert.FromBase64String(encodedUsernamePassword));
			var separatorIndex = usernamePassword.IndexOf(':');

			var login = usernamePassword.Substring(0, separatorIndex);
			var password = usernamePassword.Substring(separatorIndex + 1);

			var loginIsOk = _login == login;
			if (!loginIsOk)
			{
				_logger.LogError($"HTTP Basic authentication failed for {_address}, the login was incorrect");
				return false;
			}

			var passwordIsOk = _password == password;
			if (!passwordIsOk)
			{
				_logger.LogError($"HTTP Basic authentication failed for {_address}, the password was incorrect");
				return false;
			}

			return true;
		}
	}
}