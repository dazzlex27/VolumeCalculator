﻿using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using System.Web;
using Primitives;
using Primitives.Logging;
using Primitives.Settings.Integration;
using ProcessingUtils;

namespace ExtIntegration.RequestSenders
{
	public class HttpRequestSender : IRequestSender
	{
		private readonly ILogger _logger;
		private readonly HttpClient _httpClient;
		private readonly List<string> _destinationAddresses;
		private readonly AuthenticationHeaderValue _authenticationHeaderData;

		public HttpRequestSender(ILogger logger, HttpClient httpClient, HttpRequestSettings settings)
		{
			_logger = logger;
			_httpClient = httpClient;
			var useSecureConnection = settings.UseSecureConnection;

			_destinationAddresses = new List<string>();
			foreach (var ip in settings.DestinationIps)
			{
				var prefixString = useSecureConnection ? "https" : "http";
				var portString = GetPortString(useSecureConnection, settings.Port);

				var address = $"{prefixString}://{ip}{portString}/{settings.Url}";
				_destinationAddresses.Add(address);
				_logger.LogInfo($"Creating GET request sender for {address}");
			}

			if (!string.IsNullOrEmpty(settings.Login))
			{
				_authenticationHeaderData = NetworkUtils.GetBasicAuthenticationHeaderData(settings.Login, settings.Password);
				_logger.LogInfo($"Authentication data for https requests - \"{_authenticationHeaderData.ToString()}\"");
			}
		}

		public void Dispose()
		{
		}

		public void Connect()
		{
		}

		public bool Send(CalculationResultData resultData)
		{
			try
			{
				var resultIsOk = resultData != null && resultData.Status == CalculationStatus.Successful;
				if (!resultIsOk)
				{
					var message = resultData == null ? "result was null" : resultData.Status.ToString();
					_logger.LogError($"HttpRequestSender: result was invalid: {message}, will not send request");

					return false;
				}

				var result = resultData.Result;

				var oneOfTheRequestsFailed = false;

				foreach (var address in _destinationAddresses)
				{
					try
					{
						_logger.LogInfo($"Sending GET request to {address}...");

						var builder = new UriBuilder(address);

						var query = HttpUtility.ParseQueryString(builder.Query);
						query["bar"] = result.Barcode;
						query["wt"] = result.ObjectWeight.ToString();
						query["l"] = result.ObjectLengthMm.ToString();
						query["w"] = result.ObjectWidthMm.ToString();
						query["h"] = result.ObjectHeightMm.ToString();
						query["u"] = result.UnitCount.ToString();
						query["c"] = result.CalculationComment.ToString();

						builder.Query = query.ToString();
						string finalUrl = builder.ToString();

						HttpResponseMessage response = null;

						if (_authenticationHeaderData != null)
						{
							using (var requestMessage = new HttpRequestMessage(HttpMethod.Get, finalUrl))
							{
								requestMessage.Headers.Authorization = _authenticationHeaderData;
								response = Task.Run(() => _httpClient.SendAsync(requestMessage)).Result;
							}
						}
						else
							response = Task.Run(() => _httpClient.GetAsync(finalUrl)).Result;

						_logger.LogInfo($"Sent GET request - {finalUrl}, response was {response}");
					}
					catch (Exception ex)
					{
						_logger.LogException($"Failed to send GET request to {address}", ex);

						oneOfTheRequestsFailed = true;
					}
				}

				return !oneOfTheRequestsFailed;
			}
			catch (Exception ex)
			{
				_logger.LogException($"Failed to send GET request", ex);

				return false;
			}
		}

		private string GetPortString(bool secureConnection, int port)
		{
			if (secureConnection && port == 443)
				return "";

			if (!secureConnection && port == 80)
				return "";

			return $":{port}";
		}
	}
}