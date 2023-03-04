using DeviceIntegration;
using Primitives;

namespace FrameProviders.D435
{
	internal class PluginDefinition : IPlugin
	{
		public void Initialize()
		{
			DeviceIntegrationCommon.RegisterFrameProvider("kinectv2", typeof(RealsenseD435FrameProvider));
		}
	}
}
