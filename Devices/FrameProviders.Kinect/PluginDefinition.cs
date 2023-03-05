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
			DeviceRegistrator.RegisterFrameProvider("kinectv2", typeof(KinectV2FrameProvider));
		}
	}
}
