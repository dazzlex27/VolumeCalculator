using MySql.Data.MySqlClient;
using System;
using System.Data;
namespace SqlSender
{
	internal class SqlSender
	{
		private readonly MySqlConnection _connection;
		private readonly string _insertionSqlRequest;

		public SqlSender(string hostName)
		{
			const string hostname = "106.109.32.201";
			const string dbName = "innovation";
			const string username = "cargo";
			const string password = "cargo123";
			const string tableName = "EXPORT_FROM_INSIZE";

			var connectionString = $"server={hostname};port={3306};database={dbName}; UID={username}; password={password}";

			_insertionSqlRequest = $"INSERT INTO {tableName} (WEIGHT, LENGTH, WIDTH, HEIGHT, BARCODE) VALUES (@weight, @length, @width, @height, @barcode);";

			_connection = new MySqlConnection(connectionString);
		}

		public bool Send()
		{
			try
			{
				if (_connection.State != ConnectionState.Open)
				{
					Console.WriteLine($"Restoring SQL connection to {_connection.ConnectionString}...");
					_connection.Open();
					Console.WriteLine("Connection successful!");
				}

				Console.ReadKey();

				Console.WriteLine($"Sending SQL request to {_connection.ConnectionString}...");

				using (var command = new MySqlCommand(_insertionSqlRequest, _connection))
				{
					command.Parameters.AddWithValue("@weight", 1);
					command.Parameters.AddWithValue("@length", 2);
					command.Parameters.AddWithValue("@width", 3);
					command.Parameters.AddWithValue("@height", 4);
					command.Parameters.AddWithValue("@barcode", "test");
					var affectedRowsCount = command.ExecuteNonQuery();
					Console.WriteLine($"Sent SQL request to {_connection.ConnectionString}, {affectedRowsCount} rows affected");
				}

				return true;
			}
			catch (Exception ex)
			{
				Console.WriteLine($"{ex}: Failed to send an SQL request ({_connection.ConnectionString})");
				return false;
			}
		}
	}
}