using System;
using System.Collections.Generic;
using System.Globalization;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using Primitives.Logging;
using Primitives.Settings.Integration;
using ProcessingUtils;
using Microsoft.AspNetCore.WebUtilities;
using Primitives.Calculation;

namespace ExtIntegration.RequestSenders
{
	public sealed class HttpRequestSender : IRequestSender
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

			if (string.IsNullOrEmpty(settings.Login))
				return;
			
			_authenticationHeaderData = NetworkUtils.GetBasicAuthenticationHeaderData(settings.Login, settings.Password);
			_logger.LogInfo($"Authentication data for https requests - \"{_authenticationHeaderData}\"");
		}

		public void Dispose()
		{
		}

		public async Task ConnectAsync()
		{
			await Task.FromResult(true);
		}

		public async Task<bool> SendAsync(CalculationResultData resultData)
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

						var query = QueryHelpers.ParseQuery(builder.Query);
						query["bar"] = result.Barcode;
						query["wt"] = result.ObjectWeight.ToString(CultureInfo.InvariantCulture);
						query["l"] = result.ObjectLengthMm.ToString();
						query["w"] = result.ObjectWidthMm.ToString();
						query["h"] = result.ObjectHeightMm.ToString();
						query["u"] = result.UnitCount.ToString();
						query["c"] = result.CalculationComment;

						builder.Query = query.ToString();
						string finalUrl = builder.ToString();

						HttpResponseMessage response;

						if (_authenticationHeaderData != null)
						{
							using var requestMessage = new HttpRequestMessage(HttpMethod.Get, finalUrl);
							requestMessage.Headers.Authorization = _authenticationHeaderData;
							response = await _httpClient.SendAsync(requestMessage);
						}
						else
							response = await _httpClient.GetAsync(finalUrl);

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

		private static string GetPortString(bool secureConnection, int port)
		{
			switch (secureConnection)
			{
				case true when port == 443:
				case false when port == 80:
					return "";
				default:
					return $":{port}";
			}
		}
	}
}
