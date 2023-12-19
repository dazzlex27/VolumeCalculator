using System;
using System.ComponentModel;
using System.Windows;

namespace GuiCommon.Localization
{
	public class TranslationData : IWeakEventListener, INotifyPropertyChanged, IDisposable
	{
		private readonly string _key;

		public event PropertyChangedEventHandler PropertyChanged;

		public TranslationData(string key)
		{
			_key = key;
			LanguageChangedEventManager.AddListener(TranslationManager.Instance, this);
		}

		~TranslationData()
		{
			Dispose(false);
		}

		public object Value
		{
			get { return TranslationManager.Instance.Translate(_key); }
		}

		public void Dispose()
		{
			Dispose(true);
			GC.SuppressFinalize(this);
		}

		public bool ReceiveWeakEvent(Type managerType, object sender, EventArgs e)
		{
			if (managerType == typeof(LanguageChangedEventManager))
			{
				OnLanguageChanged(sender, e);
				return true;
			}

			return false;
		}

		protected virtual void Dispose(bool disposing)
		{
			if (disposing)
				LanguageChangedEventManager.RemoveListener(TranslationManager.Instance, this);
		}

		private void OnLanguageChanged(object sender, EventArgs e)
		{
			PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Value)));
		}
	}
}
