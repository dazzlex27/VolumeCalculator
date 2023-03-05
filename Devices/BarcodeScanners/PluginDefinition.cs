using DeviceIntegration;
using Primitives;
using System.ComponentModel.Composition;

namespace BarcodeScanners
{
	[Export(typeof(IPlugin))]
	internal class PluginDefinition : IPlugin
	{
		public void Initialize()
		{
			DeviceRegistrator.RegisterBarcodeScanner("generic", typeof(GenericSerialPortBarcodeScanner));
			DeviceRegistrator.RegisterBarcodeScanner("keyboard", typeof(GenericKeyboardBarcodeScanner));
		}
	}
}
