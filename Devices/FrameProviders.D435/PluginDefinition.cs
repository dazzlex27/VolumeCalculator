using Primitives;
using Primitives.Plugins;
using System.ComponentModel.Composition;

namespace FrameProviders.D435
{
	[Export(typeof(IPlugin))]
	internal class PluginDefinition : IPlugin
	{
		public string Type => "device";

		public void Initialize(IPluginToolset toolset)
		{
			toolset.DeviceRegistrator.RegisterDevice(DeviceType.DepthCamera, "d435", typeof(RealsenseD435FrameProvider));
		}
	}
}
