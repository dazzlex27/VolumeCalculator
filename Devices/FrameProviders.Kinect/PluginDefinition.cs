using Primitives;
using Primitives.Plugins;
using System.ComponentModel.Composition;

namespace FrameProviders.Kinect
{
	[Export(typeof(IPlugin))]
	internal class PluginDefinition : IPlugin
	{
		public string Type => "device";

		public void Initialize(IPluginToolset toolset)
		{
			toolset.DeviceRegistrator.RegisterDevice(DeviceType.DepthCamera, "kinectv2", typeof(KinectV2FrameProvider));
		}
	}
}
