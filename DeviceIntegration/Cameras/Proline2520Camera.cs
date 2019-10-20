using Primitives;
using Primitives.Logging;
using Primitives.Settings;
using System;
using System.Net.Http;
using System.Threading.Tasks;

namespace DeviceIntegration.Cameras
{
	internal class Proline2520Camera : IIpCamera
	{
		private readonly ILogger _logger;
		private readonly IpCameraSettings _settings;
		private readonly Proline2520Controller _controller;
		private bool _initialized;

		public Proline2520Camera(ILogger logger, HttpClient httpClient, IpCameraSettings settings)
		{
			_logger = logger;
			_settings = settings;
			_logger.LogInfo($"Creating a Proline2520 camera on address {_settings.Ip}... ");

			_controller = new Proline2520Controller(logger, httpClient);
		}

		public bool Initialized()
		{
			return _initialized;
		}

		public async Task<bool> ConnectAsync()
		{
			if (_initialized)
				return true;

			try
			{
				_logger.LogInfo($"Connecting to Proline2520 camera on address {_settings.Ip}... ");

				await _controller.ConnectAsync( _settings.Ip, _settings.Login, _settings.Password);

				_logger.LogInfo($"Connected to Proline2520 camera on address {_settings.Ip} ");
				_initialized = true;
			}
			catch (Exception ex)
			{
				_logger.LogException($"Failed to connect to Proline2520 camera on address {_settings.Ip} ", ex);
				_initialized = false;
			}

			return _initialized;
		}

		public async Task<bool> DisconnectAsync()
		{
			await Task.Delay(1);

			_logger.LogInfo($"Disconnecting from Proline2520 camera on address {_settings.Ip}... ");
			_initialized = false;
			return true;
		}

		public async Task<ImageData> GetSnaphostAsync()
		{
			return await _controller.GetSnapshotAsync();
		}

		public async Task<bool> GoToPresetAsync(int presetIndex)
		{
			try
			{
				await _controller.GoToPresetAsync(presetIndex);
				return true;
			}
			catch (Exception ex)
			{
				_logger.LogException($"Failed to go to preset {presetIndex}", ex);
				return false;
			}
		}
	}
}