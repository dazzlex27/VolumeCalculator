using Primitives.Plugins;
using Primitives;
using System.ComponentModel.Composition;

namespace BarcodeScanners
{
	[Export(typeof(IPlugin))]
	internal class PluginDefinition : IPlugin
	{
		public string Type => "device";

		public void Initialize(IPluginToolset toolset)
		{
			var registrator = toolset.DeviceRegistrator;
			registrator.RegisterDevice(DeviceType.BarcodeScanner, "generic", typeof(GenericSerialPortBarcodeScanner));
			registrator.RegisterDevice(DeviceType.BarcodeScanner, "keyboard", typeof(GenericKeyboardBarcodeScanner));
		}
	}
}
