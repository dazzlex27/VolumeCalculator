using Primitives;
using Primitives.Plugins;

namespace CommonUtils.Plugins
{
	public class PluginToolset : IPluginToolset
	{
		public PluginToolset(IDeviceRegistrator registrator)
		{
			DeviceRegistrator = registrator;
		}

		public IDeviceRegistrator DeviceRegistrator { get; }
	}
}
