using System.Data;
using System.Data.SqlClient;
using System.Threading.Tasks;
using Primitives;
using Primitives.Settings.Integration;

namespace ExtIntegration.RequestSenders.SqlSenders
{
	internal class MsSqlRequestEngine : ISqlRequestSenderEngine
	{
		private readonly SqlConnection _connection;
		private readonly string _insertionSqlRequestCommand;

		public MsSqlRequestEngine(SqlRequestSettings settings, string command)
		{
			_insertionSqlRequestCommand = command;

			var builder = new SqlConnectionStringBuilder
			{
				DataSource = settings.HostName,
				UserID = settings.Username,
				Password = settings.Password,
				InitialCatalog = settings.DbName
			};

			_connection = new SqlConnection(builder.ConnectionString);
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

			using (var command = new SqlCommand(_insertionSqlRequestCommand, _connection))
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
