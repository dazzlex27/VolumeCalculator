using DeviceIntegration;
using IoCircuits;
using Primitives;

namespace Scales
{
	internal class PluginDefinition : IPlugin
	{
		public void Initialize()
		{
			DeviceIntegrationCommon.RegisterIoCircuit("keusb24r", typeof(KeUsb24RCircuit));
		}
	}
}
