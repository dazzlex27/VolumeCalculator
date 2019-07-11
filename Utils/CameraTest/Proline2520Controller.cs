using Primitives;
using System;
using System.Drawing;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;

namespace CameraTest
{
	internal class Proline2520Controller
	{
		private readonly HttpClient _httpClient;
		private string _ip;
		private string _login;
		private string _password;

		public Proline2520Controller(HttpClient httpClient)
		{
			_httpClient = httpClient;
		}

		public async Task<bool> InitializeAsync(string ip, string login, string password)
		{
			_ip = ip;
			_login = login;
			_password = password;
			return await ConnectAsync();
		}

		public async Task<bool> ConnectAsync()
		{
			try
			{
				var base64String = Convert.ToBase64String(Encoding.ASCII.GetBytes($"{_login}:{_password}"));
				_httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", base64String);

				var result = await _httpClient.GetAsync(_ip);

				return result.StatusCode == HttpStatusCode.OK;
			}
			catch (Exception ex)
			{
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
				return false;
			}
		}

		public async Task<bool> SetPresetAsync(int presetIndex)
		{
			try
			{
				var url = $"http://{_ip}/web/cgi-bin/hi3510/param.cgi?cmd=preset&-act=set&-status=1&-number={presetIndex}";
				var result = await _httpClient.GetAsync(url);

				return result.StatusCode == HttpStatusCode.OK;
			}
			catch (Exception ex)
			{
				return false;
			}
		}
	}
}