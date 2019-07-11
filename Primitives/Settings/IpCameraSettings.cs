namespace Primitives.Settings
{
	public class IpCameraSettings
	{
		public string CameraName { get; set; }

		public string Ip { get; set; }

		public string Login { get; set; }

		public string Password { get; set; }

		public int ActivePreset { get; set; }

		public IpCameraSettings(string cameraName, string ip, string login, string password, int activePreset)
		{
			CameraName = cameraName;
			Ip = ip;
			Login = login;
			Password = password;
			ActivePreset = activePreset;
		}

		public static IpCameraSettings GetDefaultSettings()
		{
			return new IpCameraSettings("", "127.0.0.1", "admin", "admin", 2);
		}
	}
}