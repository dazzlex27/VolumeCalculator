using System.Collections.ObjectModel;
using GuiCommon;
using Primitives.Settings;

namespace VCConfigurator
{
	internal class IpCameraSettingsVm : BaseViewModel
	{
		private ObservableCollection<string> _ipCameraNames;

		private string _activeIpCameraName;
		private string _ipCameraAddress;
		private string _ipCameraLogin;
		private string _ipCameraPassword;
		private int _ipCameraPreset;

		public ObservableCollection<string> IpCameraNames
		{
			get => _ipCameraNames;
			set => SetField(ref _ipCameraNames, value, nameof(IpCameraNames));
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

		public IpCameraSettingsVm()
		{
			IpCameraNames = new ObservableCollection<string> { "", "proline2520" };
		}

		public void FillValuesFromSettings(IoSettings settings)
		{
			ActiveIpCameraName = settings.IpCameraSettings.CameraName;
			IpCameraAddress = settings.IpCameraSettings.Ip;
			IpCameraLogin = settings.IpCameraSettings.Login;
			IpCameraPassword = settings.IpCameraSettings.Password;
			IpCameraPreset = settings.IpCameraSettings.ActivePreset + 1;
		}

		public void FillSettingsFromValues(IoSettings settings)
		{
			settings.IpCameraSettings.CameraName = ActiveIpCameraName;
			settings.IpCameraSettings.Ip = IpCameraAddress;
			settings.IpCameraSettings.Login= IpCameraLogin;
			settings.IpCameraSettings.Password = IpCameraPassword;
			settings.IpCameraSettings.ActivePreset = IpCameraPreset - 1;
        }
	}
}