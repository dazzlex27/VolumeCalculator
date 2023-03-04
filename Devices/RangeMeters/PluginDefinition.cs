using DeviceIntegration;
using Primitives;
using System.ComponentModel.Composition;

namespace RangeMeters
{
	[Export(typeof(IPlugin))]
	internal class PluginDefinition : IPlugin
	{
		public void Initialize()
		{
			DeviceIntegrationCommon.RegisterRangeMeter("custom", typeof(TeslaM70RangeMeter));
			DeviceIntegrationCommon.RegisterRangeMeter("fake", typeof(FakeRangeMeter));
		}
	}
}
