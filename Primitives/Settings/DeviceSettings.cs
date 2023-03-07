namespace Primitives.Settings
{
	public class DeviceSettings
	{
		public string Name { get; set; }

		public string Port { get; set; }

		public DeviceSettings(string name, string port)
		{
			Name = name;
			Port = port;
		}

		public override string ToString()
		{
			return $"Name={Name},Port={Port}";
		}
	}
}
