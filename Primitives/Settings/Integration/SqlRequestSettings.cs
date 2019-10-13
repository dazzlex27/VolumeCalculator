namespace Primitives.Settings.Integration
{
	public class SqlRequestSettings
	{
		public bool EnableRequests { get; set; }

		public SqlProvider Provider { get; set; }

		public string HostName { get; set; }

		public string Username { get; set; }

		public string Password { get; set; }

		public string DbName { get; set; }

		public string TableName { get; set; }

		public SqlRequestSettings(bool enableRequests, SqlProvider provider, string hostName, string username, 
			string password, string dbName, string tableName)
		{
			EnableRequests = enableRequests;
			Provider = provider;
			HostName = hostName;
			Username = username;
			Password = password;
			DbName = dbName;
			TableName = tableName;
		}

		public static SqlRequestSettings GetDefaultSettings()
		{
			return new SqlRequestSettings(false, SqlProvider.MsSqlServer, "localhost", "sa", "Password123", "db1", "table1");
		}
	}
}