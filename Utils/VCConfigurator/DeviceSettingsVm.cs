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
		private ObservableCollection<string> _ipCameraNames;
		private string _activeScalesName;
		private string _activeIoCircuitName;
		private string _activeRangeMeterName;
		private string _activeCameraName;
		private string _activeIpCameraName;
		private string _ipCameraAddress;
		private string _ipCameraLogin;
		private string _ipCameraPassword;
		private int _ipCameraPreset;
		private string _scalesPort;
		private string _ioCircuitPort;
		private string _rangeMeterPort;
        private int _scalesMinWeight;

		public ObservableCollection<string> ScalesNames
		{
			get => _scalesNames;
			set => SetField(ref _scalesNames, value, nameof(ScalesNames));
		}

		public ObservableCollection<string> IoCircuitNames
		{
			get => _ioCircuitNames;
			set => SetField(ref _ioCircuitNames, value, nameof(IoCircuitNames));
		}

		public ObservableCollection<string> RangeMeterNames
		{
			get => _rangeMeterNames;
			set => SetField(ref _rangeMeterNames, value, nameof(RangeMeterNames));
		}

		public ObservableCollection<string> CameraNames
		{
			get => _cameraNames;
			set => SetField(ref _cameraNames, value, nameof(CameraNames));
		}

		public ObservableCollection<string> IpCameraNames
		{
			get => _ipCameraNames;
			set => SetField(ref _ipCameraNames, value, nameof(IpCameraNames));
		}

		public string ActiveScalesName
		{
			get => _activeScalesName;
			set => SetField(ref _activeScalesName, value, nameof(ActiveScalesName));
		}

		public string ActiveIoCircuitName
		{
			get => _activeIoCircuitName;
			set => SetField(ref _activeIoCircuitName, value, nameof(ActiveIoCircuitName));
		}

		public string ActiveRangeMeterName
		{
			get => _activeRangeMeterName;
			set => SetField(ref _activeRangeMeterName, value, nameof(ActiveRangeMeterName));
		}

		public string ActiveCameraName
		{
			get => _activeCameraName;
			set => SetField(ref _activeCameraName, value, nameof(ActiveCameraName));
		}

		public string ActiveIpCameraName
		{
			get => _activeIpCameraName;
			set => SetField(ref _activeIpCameraName, value, nameof(ActiveIpCameraName));
		}

		public string IpCameraAddress
		{
			get => _ipCameraAddress;
			set => SetField(ref _ipCameraAddress, value, nameof(IpCameraAddress));
		}

		public string IpCameraLogin
		{
			get => _ipCameraLogin;
			set => SetField(ref _ipCameraLogin, value, nameof(IpCameraLogin));
		}

		public string IpCameraPassword
		{
			get => _ipCameraPassword;
			set => SetField(ref _ipCameraPassword, value, nameof(IpCameraPassword));
		}

		public int IpCameraPreset
		{
			get => _ipCameraPreset;
			set => SetField(ref _ipCameraPreset, value, nameof(IpCameraPreset));
		}

		public string ScalesPort
		{
			get => _scalesPort;
			set => SetField(ref _scalesPort, value, nameof(ScalesPort));
		}

		public string IoCircuitPort
		{
			get => _ioCircuitPort;
			set => SetField(ref _ioCircuitPort, value, nameof(IoCircuitPort));
		}

		public string RangeMeterPort
		{
			get => _rangeMeterPort;
			set => SetField(ref _rangeMeterPort, value, nameof(RangeMeterPort));
		}

        public int ScalesMinWeight
        {
            get => _scalesMinWeight;
            set => SetField(ref _scalesMinWeight, value, nameof(ScalesMinWeight));
        }

        public DeviceSettingsVm()
		{
			ScalesNames = new ObservableCollection<string> { "", "massak", "casm", "fakescales", "ci2001a", "oka" };
			IoCircuitNames = new ObservableCollection<string> { "", "keusb24r" };
			RangeMeterNames = new ObservableCollection<string> { "", "custom", "fake" };
			CameraNames = new ObservableCollection<string> { "kinectv2", "d435", "local" };
			IpCameraNames = new ObservableCollection<string> { "", "proline2520" };
		}

		public void FillValuesFromSettings(IoSettings settings)
		{
			ActiveScalesName = settings.ActiveScalesName;
			ActiveIoCircuitName = settings.ActiveIoCircuitName;
			ActiveRangeMeterName = settings.ActiveRangeMeterName;
			ActiveCameraName = settings.ActiveCameraName;
			ActiveIpCameraName = settings.IpCameraSettings.CameraName;
			IpCameraAddress = settings.IpCameraSettings.Ip;
			IpCameraLogin = settings.IpCameraSettings.Login;
			IpCameraPassword = settings.IpCameraSettings.Password;
			IpCameraPreset = settings.IpCameraSettings.ActivePreset + 1;
			ScalesPort = settings.ScalesPort;
			IoCircuitPort = settings.IoCircuitPort;
			RangeMeterPort = settings.RangeMeterPort;
            ScalesMinWeight = settings.ScalesMinWeight;
		}

		public void FillSettingsFromValues(IoSettings settings)
		{
			settings.ActiveScalesName = ActiveScalesName;
			settings.ActiveIoCircuitName = ActiveIoCircuitName;
			settings.ActiveRangeMeterName = ActiveRangeMeterName;
			settings.ActiveCameraName = ActiveCameraName;
			settings.IpCameraSettings.CameraName = ActiveIpCameraName;
			settings.IpCameraSettings.Ip = IpCameraAddress;
			settings.IpCameraSettings.Login= IpCameraLogin;
			settings.IpCameraSettings.Password = IpCameraPassword;
			settings.IpCameraSettings.ActivePreset = IpCameraPreset - 1;
			settings.ScalesPort = ScalesPort;
			settings.IoCircuitPort = IoCircuitPort;
			settings.RangeMeterPort = RangeMeterPort;
            settings.ScalesMinWeight = ScalesMinWeight;
        }
	}
}