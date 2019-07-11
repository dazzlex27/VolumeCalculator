﻿using Primitives;
using Primitives.Logging;
using System;
using System.Drawing;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
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

		public async Task<bool> ConnectAsync(string ip, string login, string password)
		{
			try
			{
				_ip = ip;
				_login = login;
				_password = password;

				var base64String = Convert.ToBase64String(Encoding.ASCII.GetBytes($"{_login}:{_password}"));
				_httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", base64String);

				var result = await _httpClient.GetAsync(_ip);

				return result.StatusCode == HttpStatusCode.OK;
			}
			catch (Exception ex)
			{
				_logger.LogException($"Failed to connect to Proline2520 camera on {_ip}", ex);
				return false;
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
				_logger.LogException($"Failed to get snapshot from Proline2520 camera on {_ip}", ex);
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
				_logger.LogException($"Failed to tilt up Proline2520 camera on {_ip}", ex);
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
				_logger.LogException($"Failed to tilt down Proline2520 camera on {_ip}", ex);
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
				_logger.LogException($"Failed to pan left Proline2520 camera on {_ip}", ex);
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
				_logger.LogException($"Failed to pan right Proline2520 camera on {_ip}", ex);
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
				_logger.LogException($"Failed to stop Proline2520 camera on {_ip}", ex);
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
				_logger.LogException($"Failed to zoom in Proline2520 camera on {_ip}", ex);
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
				_logger.LogException($"Failed to zoom out Proline2520 camera on {_ip}", ex);
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
				_logger.LogException($"Failed to go to preset {presetIndex} on Proline2520 camera on {_ip}", ex);
				return false;
			}
		}
	}
}