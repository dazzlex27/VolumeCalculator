using DeviceIntegration;
using Primitives;

namespace RangeMeters
{
	internal class PluginDefinition : IPlugin
	{
		public void Initialize()
		{
			DeviceIntegrationCommon.RegisterRangeMeter("custom", typeof(TeslaM70RangeMeter));
			DeviceIntegrationCommon.RegisterRangeMeter("fake", typeof(FakeRangeMeter));
		}
	}
}
