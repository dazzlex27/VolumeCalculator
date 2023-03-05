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
			DeviceRegistrator.RegisterRangeMeter("custom", typeof(TeslaM70RangeMeter));
			DeviceRegistrator.RegisterRangeMeter("fake", typeof(FakeRangeMeter));
		}
	}
}
