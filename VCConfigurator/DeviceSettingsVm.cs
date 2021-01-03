using System.Collections.ObjectModel;
using GuiCommon;
using Primitives.Settings;

namespace VCConfigurator
{
	internal class DeviceSettingsVm : BaseViewModel
	{
		private ObservableCollection<string> _ioCircuitNames;
		private ObservableCollection<string> _rangeMeterNames;
		private ObservableCollection<string> _cameraNames;

		private ScalesSettingsVm _scalesSettings;
		private IpCameraSettingsVm _ipCameraSettings;
		
		private string _activeIoCircuitName;
		private string _activeRangeMeterName;
		private string _activeCameraName;
		private string _ioCircuitPort;

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

		public ScalesSettingsVm ScalesSettings
		{
			get => _scalesSettings;
			set => SetField(ref _scalesSettings, value, nameof(ScalesSettings));
		}

		public IpCameraSettingsVm IpCameraSettings
		{
			get => _ipCameraSettings;
			set => SetField(ref _ipCameraSettings, value, nameof(IpCameraSettings));
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

		public string IoCircuitPort
		{
			get => _ioCircuitPort;
			set => SetField(ref _ioCircuitPort, value, nameof(IoCircuitPort));
		}

		public DeviceSettingsVm()
		{
			IoCircuitNames = new ObservableCollection<string> { "", "keusb24r" };
			RangeMeterNames = new ObservableCollection<string> { "", "custom", "fake" };
			CameraNames = new ObservableCollection<string> { "kinectv2", "d435", "local" };

			ScalesSettings = new ScalesSettingsVm();
			IpCameraSettings = new IpCameraSettingsVm();
		}

		public void FillValuesFromSettings(IoSettings settings)
		{
			ActiveCameraName = settings.ActiveCameraName;
			
			ScalesSettings.FillValuesFromSettings(settings.ActiveScales);
			
			ActiveIoCircuitName = settings.ActiveIoCircuit.Name;
			IoCircuitPort = settings.ActiveIoCircuit.Port;
			
			ActiveRangeMeterName = settings.ActiveRangeMeterName;
			
			IpCameraSettings.FillValuesFromSettings(settings);
		}

		public void FillSettingsFromValues(IoSettings settings)
		{
			settings.ActiveCameraName = ActiveCameraName;
			
			ScalesSettings.FillSettingsFromValues(settings.ActiveScales);
			
			settings.ActiveIoCircuit.Name = ActiveIoCircuitName;
			settings.ActiveIoCircuit.Port = IoCircuitPort;
			
			settings.ActiveRangeMeterName = ActiveRangeMeterName;

			IpCameraSettings.FillSettingsFromValues(settings);
		}
	}
}