using System.Collections.ObjectModel;
using Primitives.Settings;

namespace VCConfigurator
{
	internal class DeviceSettingsVm : BaseViewModel
	{
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

		public DeviceSettingsVm()
		{
			ScalesNames = new ObservableCollection<string> { "", "massak", "casm", "fakescales", "ci2001a" };
			IoCircuitNames = new ObservableCollection<string> { "", "keusb24r" };
			RangeMeterNames = new ObservableCollection<string> { "", "custom" };
			CameraNames = new ObservableCollection<string> { "kinectv2", "d435", "local" };
		}

		public void FillValuesFromSettings(IoSettings settings)
		{
			ActiveScalesName = settings.ActiveScalesName;
			ActiveIoCircuitName = settings.ActiveIoCircuitName;
			ActiveRangeMeterName = settings.ActiveRangeMeterName;
			ActiveCameraName = settings.ActiveCameraName;
			ScalesPort = settings.ScalesPort;
			IoCircuitPort = settings.IoCircuitPort;
			RangeMeterPort = settings.RangeMeterPort;
		}

		public void FillSettingsFromValues(IoSettings settings)
		{
			settings.ActiveScalesName = ActiveScalesName;
			settings.ActiveIoCircuitName = ActiveIoCircuitName;
			settings.ActiveRangeMeterName = ActiveRangeMeterName;
			settings.ActiveCameraName = ActiveCameraName;
			settings.ScalesPort = ScalesPort;
			settings.IoCircuitPort = IoCircuitPort;
			settings.RangeMeterPort = RangeMeterPort;
		}
	}
}
