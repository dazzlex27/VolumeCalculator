using System;
using System.Collections.Generic;
using System.Net;
using Primitives;
using Primitives.Logging;
using Primitives.Settings.Integration;

namespace ExtIntegration.RequestSenders
{
	public class HttpRequestSender : IRequestSender
	{
		private readonly ILogger _logger;
		private readonly List<string> _destinationAddresses;
		private readonly string _login;
		private readonly string _password;

		public HttpRequestSender(ILogger logger, HttpRequestSettings settings)
		{
			_logger = logger;

			_destinationAddresses = new List<string>();
			foreach (var ip in settings.DestinationIps)
			{
				var address = $"http://{ip}:{settings.Port}/{settings.Url}";
				_destinationAddresses.Add(address);
				_logger.LogInfo($"Creating GET request sender for {address}");
			}

			_login = settings.Login;
			_password = settings.Password;
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
				var resultIsOk = resultData != null && resultData.Status == CalculationStatus.Sucessful;
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

						var uri = new Uri(address);

						using (var webClient = new WebClient())
						{
							var authenticationRequired = !string.IsNullOrEmpty(_login);
							if (authenticationRequired)
							{
								var credentialCache = new CredentialCache
								{
									{
										new Uri(uri.GetLeftPart(UriPartial.Authority)), "Basic",
										new NetworkCredential(_login, _password)
									}
								};

								webClient.UseDefaultCredentials = true;
								webClient.Credentials = credentialCache;
							}

							webClient.QueryString.Add("bar", result.ObjectCode);
							webClient.QueryString.Add("wt", ((int) (result.ObjectWeightKg * 1000)).ToString());
							webClient.QueryString.Add("l", result.ObjectLengthMm.ToString());
							webClient.QueryString.Add("w", result.ObjectWidthMm.ToString());
							webClient.QueryString.Add("h", result.ObjectHeightMm.ToString());
							var response = webClient.DownloadString(address);

							_logger.LogInfo($"Sent GET request to {address}, response was {response}");
						}
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
	}
}