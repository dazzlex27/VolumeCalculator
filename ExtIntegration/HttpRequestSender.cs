using Primitives.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using Primitives;
using Primitives.Settings;

namespace ExtIntegration
{
	public class HttpRequestSender : IRequestSender
	{
		private readonly ILogger _logger;
		private readonly string _fullAddress;

		public HttpRequestSender(ILogger logger, WebRequestSettings settings)
		{
			_logger = logger;

			if (settings.Port <= 0)
				throw new InvalidDataException($"{settings.Port} is not a valid port");

			_fullAddress = $"http://{settings.Address}:{settings.Port}";
			if (!IsUrlValid(_fullAddress))
				throw new InvalidDataException($"{_fullAddress} is not a valid url");
		}

		public void Dispose()
		{
		}

		public void Connect()
		{
			_logger.LogInfo($"Sending test request to {_fullAddress}...");
			var webClient = new WebClient();
			webClient.DownloadString(_fullAddress);
			_logger.LogInfo($"Test request to {_fullAddress} successful");
		}

		public bool Send(CalculationResult result)
		{
			try
			{
				_logger.LogInfo($"Sending GET request to {_fullAddress}...");
				var parameters = GetParametersFromCalculationResult(result);
				var webClient = new WebClient();

				foreach (var param in parameters)
					webClient.QueryString.Add(param.Name, param.Value);

				webClient.DownloadString(_fullAddress);

				_logger.LogInfo($"Sent GET request to {_fullAddress}");

				return true;
			}
			catch (Exception ex)
			{
				_logger.LogException($"Failed to send http request to {_fullAddress}", ex);
				return false;
			}

		}

		public void Disconnect()
		{
		}

		private static IEnumerable<RequestParameter> GetParametersFromCalculationResult(CalculationResult result)
		{
			return new List<RequestParameter>
			{
				new RequestParameter("weight", ((int)(result.ObjectWeightKg * 1000)).ToString()),
				new RequestParameter("length", result.ObjectLengthMm.ToString()),
				new RequestParameter("width", result.ObjectWidthMm.ToString()),
				new RequestParameter("height", result.ObjectHeightMm.ToString()),
				new RequestParameter("barcode", result.ObjectCode),
				new RequestParameter("count", result.UnitCount.ToString()),
				new RequestParameter("comment", result.CalculationComment)
			};
		}

		private static bool IsUrlValid(string source)
		{
			return Uri.TryCreate(source, UriKind.Absolute, out var uriResult) && uriResult.Scheme == Uri.UriSchemeHttp;
		}
	}
}