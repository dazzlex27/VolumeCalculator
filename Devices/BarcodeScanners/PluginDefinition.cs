using DeviceIntegration;
using Primitives;

namespace BarcodeScanners
{
	internal class PluginDefinition : IPlugin
	{
		public void Initialize()
		{
			DeviceIntegrationCommon.RegisterBarcodeScanner("generic", typeof(GenericSerialPortBarcodeScanner));
			DeviceIntegrationCommon.RegisterBarcodeScanner("keyboard", typeof(GenericKeyboardBarcodeScanner));
		}
	}
}
