using Primitives;
using DeviceIntegration.Scales;
using Primitives.Plugins;
using System.ComponentModel.Composition;

namespace Scales
{
	[Export(typeof(IPlugin))]
	internal class PluginDefinition : IPlugin
	{
		public string Type => "device";

		public void Initialize(IPluginToolset toolset)
		{
			var registrator = toolset.DeviceRegistrator;
			registrator.RegisterDevice(DeviceType.Scales, "fakescales", typeof(FakeScales));
			registrator.RegisterDevice(DeviceType.Scales, "massak", typeof(MassaKScales));
			registrator.RegisterDevice(DeviceType.Scales, "casm", typeof(CasMScales));
			registrator.RegisterDevice(DeviceType.Scales, "ci2001a", typeof(Ci2001AScales));
			registrator.RegisterDevice(DeviceType.Scales, "oka", typeof(OkaScales));
		}
	}
}
