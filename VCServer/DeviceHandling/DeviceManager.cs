using System;
using System.Net.Http;
using DeviceIntegration;
using Primitives.Logging;
using Primitives.Settings;

namespace VCServer.DeviceHandling
{
	public sealed class DeviceManager : IDisposable
	{
		private readonly ILogger _logger;

		public DeviceManager(ILogger logger, HttpClient httpClient, IoSettings ioSettings, DeviceFactory factory)
		{
			_logger = logger;

			_logger?.LogInfo("Creating device manager...");

			DeviceSet = DeviceSetFactory.CreateDeviceSet(_logger, httpClient, ioSettings, factory);
			DeviceEventGenerator = new DeviceEventGenerator(DeviceSet);
			DeviceStateUpdater = new DeviceStateUpdater(DeviceSet);

			_logger?.LogInfo("Device manager created");
		}

		public DeviceSet DeviceSet { get; private set; }

		public DeviceEventGenerator DeviceEventGenerator { get; private set; }

		public DeviceStateUpdater DeviceStateUpdater { get; private set; }

		public void Dispose()
		{
			_logger?.LogInfo("Disposing device manager...");

			DeviceEventGenerator?.Dispose();
			DeviceStateUpdater?.Dispose();
			DeviceSet?.Dispose();

			_logger?.LogInfo("Device manager disposed");
		}
	}
}
