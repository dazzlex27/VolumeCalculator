using DeviceIntegration;
using Primitives;
using System.ComponentModel.Composition;

namespace FrameProviders.Kinect
{
	[Export(typeof(IPlugin))]
	internal class PluginDefinition : IPlugin
	{
		public void Initialize()
		{
			DeviceIntegrationCommon.RegisterFrameProvider("kinectv2", typeof(KinectV2FrameProvider));
		}
	}
}
