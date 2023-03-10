using Primitives;
using Primitives.Plugins;
using System.ComponentModel.Composition;

namespace IpCameras
{
	[Export(typeof(IPlugin))]
	internal class PluginDefinition : IPlugin
	{
		public string Type => "device";

		public void Initialize(IPluginToolset toolset)
		{
			toolset.DeviceRegistrator.RegisterDevice(DeviceType.IpCamera, "proline2520", typeof(Proline2520Camera));
		}
	}
}
