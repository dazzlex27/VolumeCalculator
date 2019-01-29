namespace Primitives.Settings
{
	public class SqlRequestSettings
	{
		public bool EnableRequests { get; set; }

		public string HostName { get; set; }

		public string Username { get; set; }

		public string Password { get; set; }

		public string BaseTable { get; set; }

		public SqlRequestSettings(bool enableRequests, string hostName, string username, string password, string baseTable)
		{
			EnableRequests = enableRequests;
			HostName = hostName;
			Username = username;
			Password = password;
			BaseTable = baseTable;
		}

		public static SqlRequestSettings GetDefaultSettings()
		{
			return new SqlRequestSettings(false, "localhost", "sa", "Password123", "table1");
		}
	}
}