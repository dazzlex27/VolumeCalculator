using System.Collections.Generic;
using System.Globalization;
using System.Reflection;
using System.Resources;

namespace GuiCommon.Localization
{
	public class ResxTranslationProvider : ITranslationProvider
	{
		private readonly ResourceManager _resourceManager;

		public ResxTranslationProvider(string baseName, Assembly assembly)
		{
			_resourceManager = new ResourceManager(baseName, assembly);
		}

		public object Translate(string key)
		{
			return _resourceManager.GetString(key);
		}

		public IEnumerable<CultureInfo> Languages
		{
			get
			{
				// TODO: Resolve the available languages
				yield return new CultureInfo("en-US");
				yield return new CultureInfo("ru-RU");
			}
		}
	}
}
