using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;

namespace GuiCommon.Localization
{
	public class TranslationManager
	{
		private static TranslationManager _translationManager;

		public event EventHandler LanguageChanged;

		public CultureInfo CurrentLanguage
		{
			get { return Thread.CurrentThread.CurrentUICulture; }
			set
			{
				if (value == Thread.CurrentThread.CurrentUICulture)
					return;

				if (!Languages.Contains(value))
				{
					Thread.CurrentThread.CurrentUICulture = new CultureInfo("en-US");
					OnLanguageChanged();

					return;
				}

				Thread.CurrentThread.CurrentUICulture = value;
				OnLanguageChanged();
			}
		}

		public IEnumerable<CultureInfo> Languages
		{
			get
			{
				if (TranslationProvider != null)
					return TranslationProvider.Languages;
				
				return Enumerable.Empty<CultureInfo>();
			}
		}

		public static TranslationManager Instance
		{
			get
			{
				_translationManager ??= new TranslationManager();

				return _translationManager;
			}
		}

		public ITranslationProvider TranslationProvider { get; set; }

		public object Translate(string key)
		{
			if (TranslationProvider != null)
			{
				object translatedValue = TranslationProvider.Translate(key);
				if (translatedValue != null)
					return translatedValue;
			}

			return $"#{key}#";
		}

		private void OnLanguageChanged()
		{
			LanguageChanged?.Invoke(this, EventArgs.Empty);
		}
	}
}
