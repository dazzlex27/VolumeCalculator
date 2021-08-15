using Primitives;
using Primitives.Logging;
using ProcessingUtils;
using System;
using System.Drawing;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;

namespace DeviceIntegration.Cameras
{
	internal class Proline2520Controller
	{
		private readonly ILogger _logger;
		private readonly HttpClient _httpClient;
		private string _ip;
		private string _login;
		private string _password;

		public Proline2520Controller(ILogger logger, HttpClient httpClient)
		{
			_logger = logger;
			_httpClient = httpClient;
		}

		public async Task ConnectAsync(string ip, string login, string password)
		{
			try
			{
				await _logger.LogInfo($"{ip} {login} {password}");

				_ip = ip;
				_login = login;
				_password = password;

				var authenticationData = NetworkUtils.GetBasicAuthenticationHeaderData(login, password);

				using (var requestMessage = new HttpRequestMessage(HttpMethod.Get, $"http://{_ip}"))
				{
					requestMessage.Headers.Authorization = authenticationData;
					var response = await _httpClient.SendAsync(requestMessage);
					await _logger.LogInfo($"Camera connection status code - {response.StatusCode}");
				}
			}
			catch (Exception ex)
			{
				await _logger.LogException($"Failed to connect to Proline2520 camera on {_ip}", ex);
				return;
			}
		}

		public async Task<ImageData> GetSnapshotAsync()
		{
			try
			{
				var url = $"http://{_ip}/web/auto.jpg?-usr={_login}&-pwd={_password}";
				using (var response = await _httpClient.GetAsync(url))
				{
					if (response.StatusCode != HttpStatusCode.OK)
						return null;

					using (var inputStream = await response.Content.ReadAsStreamAsync())
					{
						using (var bmp = new Bitmap(inputStream))
						{
							return ImageUtils.GetImageDataFromBitmap(bmp);
						}
					}
				}
			}
			catch (Exception ex)
			{
				await _logger.LogException($"Failed to get snapshot from Proline2520 camera on {_ip}", ex);
				return null;
			}
		}

		public async Task<bool> TiltUpAsync()
		{
			try
			{
				var url = $"http://{_ip}/web/cgi-bin/hi3510/ptzctrl.cgi?-step=0&-act=up&-speed=45";
				var result = await _httpClient.GetAsync(url);

				return result.StatusCode == HttpStatusCode.OK;
			}
			catch (Exception ex)
			{
				await _logger.LogException($"Failed to tilt up Proline2520 camera on {_ip}", ex);
				return false;
			}
		}

		public async Task<bool> TiltDownAsync()
		{
			try
			{
				var url = $"http://{_ip}/web/cgi-bin/hi3510/ptzctrl.cgi?-step=0&-act=down&-speed=45";
				var result = await _httpClient.GetAsync(url);

				return result.StatusCode == HttpStatusCode.OK;
			}
			catch (Exception ex)
			{
				await _logger.LogException($"Failed to tilt down Proline2520 camera on {_ip}", ex);
				return false;
			}
		}

		public async Task<bool> PanLeftAsync()
		{
			try
			{
				var url = $"http://{_ip}/web/cgi-bin/hi3510/ptzctrl.cgi?-step=0&-act=left&-speed=45";
				var result = await _httpClient.GetAsync(url);

				return result.StatusCode == HttpStatusCode.OK;
			}
			catch (Exception ex)
			{
				await _logger.LogException($"Failed to pan left Proline2520 camera on {_ip}", ex);
				return false;
			}
		}

		public async Task<bool> PanRightAsync()
		{
			try
			{
				var url = $"http://{_ip}/web/cgi-bin/hi3510/ptzctrl.cgi?-step=0&-act=right&-speed=45";
				var result = await _httpClient.GetAsync(url);

				return result.StatusCode == HttpStatusCode.OK;
			}
			catch (Exception ex)
			{
				await _logger.LogException($"Failed to pan right Proline2520 camera on {_ip}", ex);
				return false;
			}
		}

		public async Task<bool> StopAsync()
		{
			try
			{
				var url = $"http://{_ip}/web/cgi-bin/hi3510/ptzctrl.cgi?-step=0&-act=stop&-speed=45";
				var result = await _httpClient.GetAsync(url);

				return result.StatusCode == HttpStatusCode.OK;
			}
			catch (Exception ex)
			{
				await _logger.LogException($"Failed to stop Proline2520 camera on {_ip}", ex);
				return false;
			}
		}

		public async Task<bool> ZoomInAsync()
		{
			try
			{
				var url = $"http://{_ip}/web/cgi-bin/hi3510/ptzctrl.cgi?-step=0&-act=zoomin&-speed=45";
				var result = await _httpClient.GetAsync(url);

				return result.StatusCode == HttpStatusCode.OK;
			}
			catch (Exception ex)
			{
				await _logger.LogException($"Failed to zoom in Proline2520 camera on {_ip}", ex);
				return false;
			}
		}

		public async Task<bool> ZoomOutAsync()
		{
			try
			{
				var url = $"http://{_ip}/web/cgi-bin/hi3510/ptzctrl.cgi?-step=0&-act=zoomout&-speed=45";
				var result = await _httpClient.GetAsync(url);

				return result.StatusCode == HttpStatusCode.OK;
			}
			catch (Exception ex)
			{
				await _logger.LogException($"Failed to zoom out Proline2520 camera on {_ip}", ex);
				return false;
			}
		}

		public async Task<bool> GoToPresetAsync(int presetIndex)
		{
			try
			{
				var url = $"http://{_ip}/web/cgi-bin/hi3510/param.cgi?cmd=preset&-act=goto&-status=1&-number={presetIndex}";
				var result = await _httpClient.GetAsync(url);

				return result.StatusCode == HttpStatusCode.OK;
			}
			catch (Exception ex)
			{
				await _logger.LogException($"Failed to go to preset {presetIndex} on Proline2520 camera on {_ip}", ex);
				return false;
			}
		}
	}
}