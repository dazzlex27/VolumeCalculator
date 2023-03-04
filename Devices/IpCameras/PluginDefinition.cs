using DeviceIntegration;
using Primitives;

namespace IpCameras
{
	internal class PluginDefinition : IPlugin
	{
		public void Initialize()
		{
			DeviceIntegrationCommon.RegisterIpCamera("proline2520", typeof(Proline2520Camera));
		}
	}
}
