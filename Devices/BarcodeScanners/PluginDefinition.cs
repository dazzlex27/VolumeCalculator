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
			DeviceIntegrationCommon.RegisterBarcodeScanner("generic", typeof(GenericSerialPortBarcodeScanner));
			DeviceIntegrationCommon.RegisterBarcodeScanner("keyboard", typeof(GenericKeyboardBarcodeScanner));
		}
	}
}
