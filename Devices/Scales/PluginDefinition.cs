using DeviceIntegration;
using DeviceIntegration.Scales;
using Primitives;
using System.ComponentModel.Composition;

namespace Scales
{
	[Export(typeof(IPlugin))]
	internal class PluginDefinition : IPlugin
	{
		public void Initialize()
		{
			DeviceIntegrationCommon.RegisterScales("fakescales", typeof(FakeScales));
			DeviceIntegrationCommon.RegisterScales("massak", typeof(MassaKScales));
			DeviceIntegrationCommon.RegisterScales("casm", typeof(CasMScales));
			DeviceIntegrationCommon.RegisterScales("ci2001a", typeof(Ci2001AScales));
			DeviceIntegrationCommon.RegisterScales("oka", typeof(OkaScales));
		}
	}
}
