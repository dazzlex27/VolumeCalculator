using Primitives;
using Primitives.Plugins;
using System.ComponentModel.Composition;

namespace IoCircuits
{
	[Export(typeof(IPlugin))]
	internal class PluginDefinition : IPlugin
	{
		public string Type => "device";

		public void Initialize(IPluginToolset toolset)
		{
			toolset.DeviceRegistrator.RegisterDevice(DeviceType.IoCircuit, "keusb24r", typeof(KeUsb24RCircuit));
		}
	}
}
