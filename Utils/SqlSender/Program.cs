using ExtIntegration.RequestSenders.SqlSenders;
using Primitives.Calculation;
using Primitives;
using Primitives.Settings;
using Primitives.Settings.Integration;
using System;
using System.Threading.Tasks;

namespace SqlSender
{
	internal class Program
	{
		private static async Task Main()
		{
			try
			{
				const string hostname = "106.109.32.201";
				const string dbName = "innovation";
				const string username = "cargo";
				const string password = "cargo123";
				const string tableName = "EXPORT_FROM_IS";

				var insertionSqlRequest = $"INSERT INTO {tableName} (DATETIME, BARCODE, WEIGHT, LENGTH, WIDTH, HEIGHT, UNITCOUNT, COMMENT) VALUES (@datetime, @barcode, @weight, @length, @width, @height, @unitcount, @comment);";

				var settings = new SqlRequestSettings(true, SqlProvider.MsSqlServer, hostname,
					username, password, dbName, tableName);
				using var sender = new MsSqlRequestEngine(settings, insertionSqlRequest);

				var result = new CalculationResult(DateTime.Now, "test", 0, WeightUnits.Kg,
					0, 0, 0, 0, 0, "test", false);
				var imageData = new ImageData(40, 30, 3);
				var resultData = new CalculationResultData(result, CalculationStatus.Successful, imageData);

				await sender.SendAsync(resultData);
			}
			catch (Exception e)
			{
				Console.WriteLine($"Failed to start the service: {e}");
			}

			Console.WriteLine("Application finished");
		}
	}
}
