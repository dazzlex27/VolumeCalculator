using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ExtIntegration.RequestHandlers;
using ExtIntegration.RequestSenders;
using ExtIntegration.RequestSenders.SqlSenders;
using Primitives;
using Primitives.Logging;
using Primitives.Settings.Integration;

namespace ExtIntegration
{
	public class RequestProcessor : IDisposable
	{
		public event Action<CalculationRequestData> StartRequestReceived;

		private readonly ILogger _logger;
		private readonly WebClientHandler _webClientHandler;
		private readonly HttpRequestHandler _httpRequestHandler;
		private readonly Queue<HttpRequestData> _activeHttpRequests;
		private readonly List<IRequestSender> _requestSenders;

		public RequestProcessor(ILogger logger, IntegrationSettings settings)
		{
			_logger = logger;
			_requestSenders = new List<IRequestSender>();
			_activeHttpRequests = new Queue<HttpRequestData>(1);

			_logger.LogInfo("Creating request processor...");

			if (settings.HttpApiSettings.EnableRequests)
			{
				_httpRequestHandler = new HttpRequestHandler(_logger, settings.HttpApiSettings);
				_httpRequestHandler.CalculationStartRequested += OnHttpStartRequestReceived;
				_httpRequestHandler.CalculationStartRequestTimedOut += OnHttpRequestHandlerTimedOut;
			}

			if (settings.WebClientHandlerSettings.EnableRequests)
			{
				_webClientHandler = new WebClientHandler(_logger, settings.WebClientHandlerSettings);
				_webClientHandler.CalculationStartRequested += OnStartRequestReceived;
			}

			_requestSenders = new List<IRequestSender>();

			if (settings.SqlRequestSettings.EnableRequests)
				_requestSenders.Add(new SqlRequestSender(_logger, settings.SqlRequestSettings));

			if (settings.HttpRequestSettings.EnableRequests)
				_requestSenders.Add(new HttpRequestSender(_logger, settings.HttpRequestSettings));

			if (settings.FtpRequestSettings.EnableRequests)
				_requestSenders.Add(new FtpRequestSender(_logger, settings.FtpRequestSettings));

			foreach (var sender in _requestSenders)
			{
				try
				{
					sender.Connect();
				}
				catch (Exception ex)
				{
					_logger.LogException("RequestProcessor: a request sender failed to connect to requered destination", ex);
				}
			}
		}

		public void Dispose()
		{
			if (_httpRequestHandler != null)
			{
				_httpRequestHandler.CalculationStartRequested -= OnHttpStartRequestReceived;
				_httpRequestHandler.CalculationStartRequestTimedOut -= OnHttpRequestHandlerTimedOut;
				_httpRequestHandler.Dispose();
			}

			if (_webClientHandler != null)
			{
				_webClientHandler.CalculationStartRequested -= OnStartRequestReceived;
				_webClientHandler.Dispose();
			}

			foreach (var sender in _requestSenders)
				sender?.Dispose();
		}

		public void SendRequests(CalculationResultData resultData)
		{
			if (_httpRequestHandler != null)
			{
				while (_activeHttpRequests.Any())
				{
					var httpContext = _activeHttpRequests.Dequeue();
					_httpRequestHandler.SendResponse(httpContext, resultData);
				}
			}

			_webClientHandler?.UpdateCalculationResult(resultData);

			foreach (var sender in _requestSenders)
			{
				Task.Run(() =>
				{
					try
					{
						var sent = sender.Send(resultData);
					}
					catch (Exception ex)
					{
						_logger.LogException("Failed to send a request!", ex);
					}

				});
			}
		}

		public void UpdateCalculationStatus(CalculationStatus status)
		{
			_webClientHandler?.UpdateCalculationStatus(status);
		}

		public void ResetRequest(HttpRequestData data, string message)
		{
			_httpRequestHandler.Reset(data, message);
		}

		private void OnHttpRequestHandlerTimedOut()
		{
			while (_activeHttpRequests.Any())
				_httpRequestHandler.Reset(_activeHttpRequests.Dequeue(), "Request timed out");
		}

		private void OnHttpStartRequestReceived(HttpRequestData data)
		{
			_activeHttpRequests.Enqueue(data);
			StartRequestReceived?.Invoke(null);
		}

		private void OnStartRequestReceived(CalculationRequestData obj)
		{
			StartRequestReceived?.Invoke(obj);
		}
	}
}