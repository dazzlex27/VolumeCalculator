using System.Data;
using System.Data.SqlClient;
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

			using (var command = new SqlCommand(_insertionSqlRequestCommand, _connection))
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