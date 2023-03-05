using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.ComponentModel.Composition.Hosting;
using System.Net.Http;
using Primitives;
using Primitives.Logging;
using Primitives.Settings;

namespace VCServer
{
	public sealed class DeviceManager : IDisposable
	{
		private ILogger _logger;

		public DeviceManager(ILogger logger, HttpClient httpClient, IoSettings ioSettings)
		{
			_logger = logger;

			_logger?.LogInfo("Creating device manager...");

			LoadPlugins();

			DeviceSet = DeviceSetFactory.CreateDeviceSet(_logger, httpClient, ioSettings);
			DeviceEventGenerator = new DeviceEventGenerator(DeviceSet);
			DeviceStateUpdater = new DeviceStateUpdater(DeviceSet);

			_logger?.LogInfo("Device manager created");
		}

		// TODO: move to server space and inject here
		[ImportMany]
		private IEnumerable<IPlugin> Plugins { get; set; }

		public DeviceSet DeviceSet { get; private set; }

		public DeviceEventGenerator DeviceEventGenerator { get; private set; }

		public DeviceStateUpdater DeviceStateUpdater { get; private set; }

		// TODO: put this into a separate entity
		public void LoadPlugins()
		{
			var catalog = new DirectoryCatalog("Plugins");
			using var container = new CompositionContainer(catalog);
			container.ComposeParts(this);

			foreach (var plugin in Plugins)
				plugin.Initialize();
		}

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
