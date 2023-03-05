using DeviceIntegration;
using Primitives;
using System.ComponentModel.Composition;

namespace IoCircuits
{
	[Export(typeof(IPlugin))]
	internal class PluginDefinition : IPlugin
	{
		public void Initialize()
		{
			DeviceRegistrator.RegisterIoCircuit("keusb24r", typeof(KeUsb24RCircuit));
		}
	}
}
