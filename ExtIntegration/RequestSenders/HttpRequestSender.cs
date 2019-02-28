using System;
using System.Net;
using Primitives;
using Primitives.Logging;
using Primitives.Settings;

namespace ExtIntegration.RequestSenders
{
	public class HttpRequestSender : IRequestSender
	{
		private readonly ILogger _logger;
		private readonly string _address;

		public HttpRequestSender(ILogger logger, HttpRequestSettings settings)
		{
			_logger = logger;
			_address = $"http://{settings.Address}:{settings.Port}/{settings.Url}";
		}

		public void Dispose()
		{
		}

		public void Connect()
		{
		}

		public bool Send(CalculationResult result)
		{
			try
			{
				_logger.LogInfo($"Sending GET request to {_address}...");

				var resultIsOk = result != null && result.Status != CalculationStatus.Sucessful;
				if (!resultIsOk)
				{
					var message = result == null ? "result was null" : result.Status.ToString();
					_logger.LogError($"HttpRequestSender: result was invalid: {message}, will not send request");

					return false;
				}

				var webClient = new WebClient();
				webClient.QueryString.Add("bar", result.ObjectCode);
				webClient.QueryString.Add("wt", ((int) (result.ObjectWeightKg * 1000)).ToString());
				webClient.QueryString.Add("l", result.ObjectLengthMm.ToString());
				webClient.QueryString.Add("w", result.ObjectWidthMm.ToString());
				webClient.QueryString.Add("h", result.ObjectHeightMm.ToString());
				var response = webClient.DownloadString(_address);

				_logger.LogInfo($"Sent GET request to {_address}, response was {response}");

				return true;
			}
			catch (Exception ex)
			{
				_logger.LogException($"Failed to send GET request to {_address}", ex);

				return false;
			}
		}

		public void Disconnect()
		{
		}
	}
}