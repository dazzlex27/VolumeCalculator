using System;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;
using Primitives.Calculation;
using Primitives.Logging;
using Primitives.Settings.Integration;

namespace ExtIntegration.RequestHandlers
{
	internal sealed class HttpRequestHandler : IDisposable
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

		private readonly CancellationTokenSource _tokenSource;

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

			_tokenSource = new CancellationTokenSource();

			Task.Factory.StartNew(async (o) =>
			{
				try
				{
					await ListenToRequestsAsync();
				}
				catch (Exception ex)
				{
					_logger.LogException("Http listening failed", ex);
				}
			}, TaskCreationOptions.LongRunning, _tokenSource.Token);
		}

		public void Dispose()
		{
			_logger.LogInfo($"Disposing http listener for {_address} ...");
			_running = false;
			_tokenSource.Dispose();
			_requestHandlingTimeoutTimer?.Dispose();
			_logger?.LogInfo($"Disposed http listener for {_address}");
		}

		public async Task SendResponse(HttpRequestData data, CalculationResultData resultData)
		{
			try
			{
				if (!_sessionInProgress)
					_logger.LogError($"Response generation call for {_address} came after the session timeout");

				_requestHandlingTimeoutTimer.Stop();

				_logger.LogInfo($"Generating HTTP response for {_address}...");
				var responseString = await RequestUtils.GenerateXmlResponseTextAsync(resultData, data.SendPhoto);
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

		private async Task SendPingResponseAsync(HttpRequestData data)
		{
			try
			{
				const string responseString = "pong";
				_logger.LogInfo($"Response text is the following: {Environment.NewLine}{responseString}");

				var response = data.Context.Response;
				var buffer = Encoding.UTF8.GetBytes(responseString);

				response.ContentLength64 = buffer.Length;
				var output = response.OutputStream;
				await output.WriteAsync(buffer);

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

		private async Task ListenToRequestsAsync()
		{
			_listener.Start();

			while (_running)
			{
				if (_sessionInProgress)
					await Task.Delay(ThreadIdleTimeSpan);

				_logger.LogInfo($"Awaiting HTTP requests from {_address}...");
				var context = await _listener.GetContextAsync();
				var request = context.Request;
				var credentialsAreOk = CheckCredentials(request);
				if (!credentialsAreOk)
				{
					_logger.LogError($"Credentials from {_address} were incorrect, will not process the request");
					continue;
				}

				var url = request.RawUrl;
				var command = url.Contains('?')
					? url.Substring(1, url.IndexOf("?", StringComparison.Ordinal))
					: url[1..];
				var method = request.HttpMethod;
				_logger.LogInfo($"HTTP request accepted from {_address} containing {url}, the method is {method}");

				_logger.LogInfo($"Received a \"{command}\" command from {_address}...");

				switch (command)
				{
					case "ping":
						await SendPingResponseAsync(new HttpRequestData(context, false));
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

			_listener.Close();
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

			var login = usernamePassword[..separatorIndex];
			var password = usernamePassword[(separatorIndex + 1)..];

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
