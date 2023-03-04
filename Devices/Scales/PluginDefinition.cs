using DeviceIntegration;
using DeviceIntegration.Scales;
using Primitives;

namespace Scales
{
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
