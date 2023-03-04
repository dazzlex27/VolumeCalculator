using DeviceIntegration;
using Primitives;

namespace FrameProviders.Kinect
{
	internal class PluginDefinition : IPlugin
	{
		public void Initialize()
		{
			DeviceIntegrationCommon.RegisterFrameProvider("kinectv2", typeof(KinectV2FrameProvider));
		}
	}
}
