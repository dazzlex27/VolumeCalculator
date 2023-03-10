using Primitives.Plugins;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.ComponentModel.Composition;
using System.ComponentModel.Composition.Hosting;

namespace CommonUtils.Plugins
{
	public class PluginFromDiskContainer : IPluginContainer
	{
		private readonly IPluginToolset _toolset;
		private readonly string _path;

		public PluginFromDiskContainer(IPluginToolset toolset, string path)
		{
			_toolset = toolset;
			_path = path;
		}

		[ImportMany]
		private IEnumerable<IPlugin> Plugins { get; set; }

		public IReadOnlyList<IPlugin> GetPlugins()
		{
			return Plugins.ToImmutableList();
		}

		public void LoadPlugins()
		{
			var catalog = new DirectoryCatalog(_path);
			using var container = new CompositionContainer(catalog);
			container.ComposeParts(this);

			foreach (var plugin in Plugins)
				plugin.Initialize(_toolset);
		}
	}
}
