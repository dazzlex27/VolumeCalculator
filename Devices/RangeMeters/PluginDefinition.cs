using Primitives;
using Primitives.Plugins;
using System.ComponentModel.Composition;

namespace RangeMeters
{
	[Export(typeof(IPlugin))]
	internal class PluginDefinition : IPlugin
	{
		public string Type => "device";

		public void Initialize(IPluginToolset toolset)
		{
			var registrator = toolset.DeviceRegistrator;
			registrator.RegisterDevice(DeviceType.RangeMeter, "custom", typeof(TeslaM70RangeMeter));
			registrator.RegisterDevice(DeviceType.RangeMeter, "fake", typeof(FakeRangeMeter));
		}
	}
}
