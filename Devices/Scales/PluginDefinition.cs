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
			DeviceRegistrator.RegisterScales("fakescales", typeof(FakeScales));
			DeviceRegistrator.RegisterScales("massak", typeof(MassaKScales));
			DeviceRegistrator.RegisterScales("casm", typeof(CasMScales));
			DeviceRegistrator.RegisterScales("ci2001a", typeof(Ci2001AScales));
			DeviceRegistrator.RegisterScales("oka", typeof(OkaScales));
		}
	}
}
