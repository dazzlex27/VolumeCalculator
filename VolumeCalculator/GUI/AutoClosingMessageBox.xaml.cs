using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Timers;
using System.Windows;

namespace VolumeCalculator.GUI
{
	internal partial class AutoClosingMessageBox : INotifyPropertyChanged
	{
		private const int DefaultClosingTimeMs = 3000;
		private const string DefaultCaptionText = "Сообщение";

		private readonly Timer _closingTimer;

		private string _text;
		private string _caption;

		public string Text
		{
			get => _text;
			set
			{
				if (_text == value)
					return;

				_text = value;
				OnPropertyChanged();
			}
		}

		public string Caption
		{
			get => _caption;
			set
			{
				if (_caption == value)
					return;

				_caption = value;
				OnPropertyChanged();
			}
		}

		private AutoClosingMessageBox()
		{
			InitializeComponent();
		}

		private AutoClosingMessageBox(string text, string caption, double timeBeforeClosingMs = DefaultClosingTimeMs)
			: this()
		{
			Text = text;
			Caption = caption == "" ? DefaultCaptionText : caption;

			_closingTimer = new Timer(timeBeforeClosingMs) { AutoReset = false };
			_closingTimer.Elapsed += OnClosingTimerElapsed;
		}

		public static void Show(string text, string caption, double timeBeforeClosing = DefaultClosingTimeMs)
		{
			var messageBox = new AutoClosingMessageBox(text, caption, timeBeforeClosing);
			messageBox.ShowDialog();
		}

		private void OnClosingTimerElapsed(object sender, ElapsedEventArgs e)
		{
			Dispatcher.Invoke(() =>
			{
				_closingTimer.Dispose();
				Close();
			});
		}

		public event PropertyChangedEventHandler PropertyChanged;

		protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
		{
			PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
		}

		private void OnWindowLoaded(object sender, RoutedEventArgs e)
		{
			_closingTimer.Start();
		}
	}
}