using DeviceIntegration;
using Primitives;
using System.ComponentModel.Composition;

namespace FrameProviders.Local
{
	[Export(typeof(IPlugin))]
	internal class PluginDefinition : IPlugin
	{
		public void Initialize()
		{
			DeviceRegistrator.RegisterFrameProvider("local", typeof(LocalFileFrameProvider));
		}
	}
}
