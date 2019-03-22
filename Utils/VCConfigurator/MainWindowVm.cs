using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using Primitives;
using Primitives.Settings;

namespace VCConfigurator
{
	internal class MainWindowVm : INotifyPropertyChanged
	{
		private readonly ApplicationSettings _settings;

		private ObservableCollection<string> _scalesNames;
		private ObservableCollection<string> _ioCircuitNames;
		private ObservableCollection<string> _rangeMeterNames;
		private ObservableCollection<string> _cameraNames;
		private string _activeScalesName;
		private string _activeIoCircuitName;
		private string _activeRangeMeterName;
		private string _activeCameraName;
		private string _scalesPort;
		private string _ioCircuitPort;
		private string _rangeMeterPort;

		public ObservableCollection<string> ScalesNames
		{
			get => _scalesNames;
			set
			{
				if (_scalesNames == value)
					return;

				_scalesNames = value;
				OnPropertyChanged();
			}
		}

		public ObservableCollection<string> IoCircuitNames
		{
			get => _ioCircuitNames;
			set
			{
				if (_ioCircuitNames == value)
					return;

				_ioCircuitNames = value;
				OnPropertyChanged();
			}
		}

		public ObservableCollection<string> RangeMeterNames
		{
			get => _rangeMeterNames;
			set
			{
				if (_rangeMeterNames == value)
					return;

				_rangeMeterNames = value;
				OnPropertyChanged();
			}
		}

		public ObservableCollection<string> CameraNames
		{
			get => _cameraNames;
			set
			{
				if (_cameraNames == value)
					return;

				_cameraNames = value;
				OnPropertyChanged();
			}
		}

		public string ActiveScalesName
		{
			get => _activeScalesName;
			set
			{
				if (_activeScalesName == value)
					return;

				_activeScalesName = value;
				OnPropertyChanged();
			}
		}

		public string ActiveIoCircuitName
		{
			get => _activeIoCircuitName;
			set
			{
				if (_activeIoCircuitName == value)
					return;

				_activeIoCircuitName = value;
				OnPropertyChanged();
			}
		}

		public string ActiveRangeMeterName
		{
			get => _activeRangeMeterName;
			set
			{
				if (_activeRangeMeterName == value)
					return;

				_activeRangeMeterName = value;
				OnPropertyChanged();
			}
		}

		public string ActiveCameraName
		{
			get => _activeCameraName;
			set
			{
				if (_activeCameraName == value)
					return;

				_activeCameraName = value;
				OnPropertyChanged();
			}
		}

		public string ScalesPort
		{
			get => _scalesPort;
			set
			{
				if (_scalesPort == value)
					return;

				_scalesPort = value;
				OnPropertyChanged();
			}
		}

		public string IoCircuitPort
		{
			get => _ioCircuitPort;
			set
			{
				if (_ioCircuitPort == value)
					return;

				_ioCircuitPort = value;
				OnPropertyChanged();
			}
		}

		public string RangeMeterPort
		{
			get => _rangeMeterPort;
			set
			{
				if (_rangeMeterPort == value)
					return;

				_rangeMeterPort = value;
				OnPropertyChanged();
			}
		}

		public ICommand ApplySettingsAndCloseCommand { get; set; }

		public ICommand ApplySettingsCommand { get; set; }

		public MainWindowVm()
		{
			ScalesNames = new ObservableCollection<string> {"", "massak", "casm", "fakescales", "ci2001a"};
			IoCircuitNames = new ObservableCollection<string> {"", "keusb24r"};
			RangeMeterNames = new ObservableCollection<string> {"", "custom"};
			CameraNames = new ObservableCollection<string> {"kinectv2", "d435", "local"};

			_settings = ReadSettingsFromFile();
			FillValuesFromSettings(_settings.IoSettings);

			ApplySettingsAndCloseCommand = new CommandHandler(() => ApplySettings(true), true);
			ApplySettingsCommand = new CommandHandler(() => ApplySettings(false), true);
		}

		public event PropertyChangedEventHandler PropertyChanged;

		protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
		{
			PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
		}

		private static ApplicationSettings ReadSettingsFromFile()
		{
			ApplicationSettings settings;

			try
			{
				var settingsFromFile = IoUtils.DeserializeSettings();
				if (settingsFromFile != null)
					return settingsFromFile;
			}
			finally
			{
				settings = ApplicationSettings.GetDefaultSettings();
			}

			return settings;
		}

		private void FillValuesFromSettings(IoSettings settings)
		{
			ActiveScalesName = settings.ActiveScalesName;
			ActiveIoCircuitName = settings.ActiveIoCircuitName;
			ActiveRangeMeterName = settings.ActiveRangeMeterName;
			ActiveCameraName = settings.ActiveCameraName;
			ScalesPort = settings.ScalesPort;
			IoCircuitPort = settings.IoCircuitPort;
			RangeMeterPort = settings.RangeMeterPort;
		}

		private void ApplySettings(bool closeApplication)
		{
			_settings.IoSettings.ActiveScalesName = ActiveScalesName;
			_settings.IoSettings.ActiveIoCircuitName = ActiveIoCircuitName;
			_settings.IoSettings.ActiveRangeMeterName = ActiveRangeMeterName;
			_settings.IoSettings.ActiveCameraName = ActiveCameraName;
			_settings.IoSettings.ScalesPort = ScalesPort;
			_settings.IoSettings.IoCircuitPort = IoCircuitPort;
			_settings.IoSettings.RangeMeterPort = RangeMeterPort;

			IoUtils.SerializeSettings(_settings);

			if (closeApplication)
				Process.GetCurrentProcess().Kill();
		}
	}
}