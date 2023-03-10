namespace Primitives.Plugins
{
	public interface IPlugin
	{
		string Type { get; }

		void Initialize(IPluginToolset toolset);
	}
}
