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