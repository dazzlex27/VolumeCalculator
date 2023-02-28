using System.Data;
using System.Threading.Tasks;
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

		public async Task ConnectAsync()
		{
			if (_connection.State != ConnectionState.Open)
				await _connection.OpenAsync();
		}

		public async Task DisconnectAsync()
		{
			await _connection.CloseAsync();
		}

		public void Dispose()
		{
			_connection?.Dispose();
		}

		public string GetConnectionString()
		{
			return _connection.ConnectionString;
		}

		public async Task<int> SendAsync(CalculationResultData resultData)
		{
			var result = resultData.Result;

			using (var command = new MySqlCommand(_insertionSqlRequestCommand, _connection))
			{
				command.Parameters.AddWithValue("@datetime", result.CalculationTime.ToString("yyyy-MM-dd HH:mm:ss"));
				command.Parameters.AddWithValue("@barcode", result.Barcode);
				command.Parameters.AddWithValue("@weight", result.ObjectWeight);
				command.Parameters.AddWithValue("@length", result.ObjectLengthMm);
				command.Parameters.AddWithValue("@width", result.ObjectWidthMm);
				command.Parameters.AddWithValue("@height", result.ObjectHeightMm);
				command.Parameters.AddWithValue("@unitcount", result.UnitCount);
				command.Parameters.AddWithValue("@comment", result.CalculationComment);
				return await command.ExecuteNonQueryAsync();
			}
		}
	}
}
