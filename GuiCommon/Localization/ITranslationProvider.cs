using System.Collections.Generic;
using System.Globalization;

namespace GuiCommon.Localization
{
	public interface ITranslationProvider
	{
		object Translate(string key);

		IEnumerable<CultureInfo> Languages { get; }

	}
}
