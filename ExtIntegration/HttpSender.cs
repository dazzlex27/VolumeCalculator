using Primitives.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using Primitives;

namespace ExtIntegration
{
	public class HttpSender
	{
		private readonly ILogger _logger;
		private readonly string _fullAddress;

		public HttpSender(ILogger logger, WebRequestSettings settings)
		{
			_logger = logger;

			if (settings.Port <= 0)
				throw new InvalidDataException($"{settings.Port} is not a valid port");

			_fullAddress = $"http://{settings.Address}:{settings.Port}";
			if (!IsUrlValid(_fullAddress))
				throw new InvalidDataException($"{_fullAddress} is not a valid url");
		}

		public bool SendSimpleRequest(CalculationResult result)
		{
			try
			{
				var parameters = GetParametersFromCalculationResult(result);
				var webClient = new WebClient();

				foreach (var param in parameters)
					webClient.QueryString.Add(param.Name, param.Value);

				webClient.DownloadString(_fullAddress);

				return true;
			}
			catch (Exception ex)
			{
				_logger.LogException($"Failed to send http request to {_fullAddress}", ex);
				return false;
			}

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