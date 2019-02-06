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

		private string _scalesPort;
		private string _ioCircuitPort;		
		private ObservableCollection<string> _scalesNames;
		private ObservableCollection<string> _boardNames;
		private ObservableCollection<string> _cameraNames;
		private string _selectedScalesName;
		private string _selectedBoardName;
		private string _selectedCameraName;

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
			get => _boardNames;
			set
			{
				if (_boardNames == value)
					return;

				_boardNames = value;
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

		public string SelectedScalesName
		{
			get => _selectedScalesName;
			set
			{
				if (_selectedScalesName == value)
					return;

				_selectedScalesName = value;
				OnPropertyChanged();
			}
		}

		public string SelectedBoardName
		{
			get => _selectedBoardName;
			set
			{
				if (_selectedBoardName == value)
					return;

				_selectedBoardName = value;
				OnPropertyChanged();
			}
		}

		public string SelectedCameraName
		{
			get => _selectedCameraName;
			set
			{
				if (_selectedCameraName == value)
					return;

				_selectedCameraName = value;
				OnPropertyChanged();
			}
		}

		public ICommand ApplySettingsAndCloseCommand { get; set; }

		public ICommand ApplySettingsCommand { get; set; }

		public MainWindowVm()
		{
			ScalesNames = new ObservableCollection<string> {"massak", "casm", "fakescales"};
			IoCircuitNames = new ObservableCollection<string> {"keusb24r"};
			CameraNames = new ObservableCollection<string> { "kinectv2", "d435", "local" };

			_settings = ReadSettingsFromFile();
			FillValuesFromSettings(_settings.IoSettings);

			ApplySettingsAndCloseCommand = new CommandHandler(() => ApplySettings(true), true);
			ApplySettingsCommand = new CommandHandler(()=>ApplySettings(false), true);
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
			SelectedScalesName = settings.ActiveScalesName;
			SelectedBoardName = settings.ActiveIoCircuitName;
			ScalesPort = settings.ScalesPort;
			IoCircuitPort = settings.IoCircuitPort;
			SelectedCameraName = settings.ActiveCameraName;
		}

		private void ApplySettings(bool closeApplication)
		{
			_settings.IoSettings.ActiveScalesName = SelectedScalesName;
			_settings.IoSettings.ActiveIoCircuitName = SelectedBoardName;
			_settings.IoSettings.ScalesPort = ScalesPort;
			_settings.IoSettings.IoCircuitPort = IoCircuitPort;
			_settings.IoSettings.ActiveCameraName = SelectedCameraName;

			IoUtils.SerializeSettings(_settings);

			if (closeApplication)
				Process.GetCurrentProcess().Kill();
		}
	}
}