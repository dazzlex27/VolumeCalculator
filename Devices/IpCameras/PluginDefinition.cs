using DeviceIntegration;
using Primitives;
using System.ComponentModel.Composition;

namespace IpCameras
{
	[Export(typeof(IPlugin))]
	internal class PluginDefinition : IPlugin
	{
		public void Initialize()
		{
			DeviceIntegrationCommon.RegisterIpCamera("proline2520", typeof(Proline2520Camera));
		}
	}
}
