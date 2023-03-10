using System.Collections.Generic;

namespace Primitives.Plugins
{
	public interface IPluginContainer
	{
		void LoadPlugins();

		IReadOnlyList<IPlugin> GetPlugins();
	}
}
