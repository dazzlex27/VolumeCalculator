using System.Data;
using MySql.Data.MySqlClient;
using Primitives;
using Primitives.Settings.Integration;

namespace ExtIntegration.RequestSenders.SqlSenders
{
	internal class MySqlRequestEngine : ISqlRequestSenderEngine
	{
		private readonly MySqlConnection _connection;
		private readonly string _insertionSqlRequestCommand;

		public MySqlRequestEngine(SqlRequestSettings settings, string command)
		{
			_insertionSqlRequestCommand = command;

			var connectionString = $"server={settings.HostName};port={3306};database={settings.DbName}; UID={settings.Username}; password={settings.Password}";

			_connection = new MySqlConnection(connectionString);
		}

		public void Connect()
		{
			if (_connection.State != ConnectionState.Open)
				_connection.Open();
		}

		public void Disconnect()
		{
			_connection.Close();
		}

		public string GetConnectionString()
		{
			return _connection.ConnectionString;
		}

		public int Send(CalculationResultData resultData)
		{
			var result = resultData.Result;

			using (var command = new MySqlCommand(_insertionSqlRequestCommand, _connection))
			{
				command.Parameters.AddWithValue("@weight", result.ObjectWeight);
				command.Parameters.AddWithValue("@length", result.ObjectLengthMm);
				command.Parameters.AddWithValue("@width", result.ObjectWidthMm);
				command.Parameters.AddWithValue("@height", result.ObjectHeightMm);
				command.Parameters.AddWithValue("@barcode", result.Barcode);
				return command.ExecuteNonQuery();
			}
		}
	}
}