using System;
using System.Threading.Tasks;
using Primitives;
using Primitives.Logging;
using Primitives.Settings.Integration;

namespace ExtIntegration.RequestSenders.SqlSenders
{
	public sealed class SqlRequestSender : IRequestSender
	{
		private readonly ILogger _logger;

		private readonly ISqlRequestSenderEngine _engine;
		private readonly string _connectionString;

		public SqlRequestSender(ILogger logger, SqlRequestSettings requestSettings)
		{
			_logger = logger;

			var insertionSqlRequest = $"INSERT INTO {requestSettings.TableName} (DATETIME, BARCODE, WEIGHT, LENGTH, WIDTH, HEIGHT, UNITCOUNT, COMMENT) VALUES (@datetime, @barcode, @weight, @length, @width, @height, @unitcount, @comment);";

			_engine = SqlRequestEngineFactory.CreateEngine(requestSettings, insertionSqlRequest);
			_connectionString = _engine.GetConnectionString();
		}

		public void Dispose()
		{
			_logger.LogInfo($"Disconnecting from {_connectionString}...");
			_engine.Dispose();
			_logger.LogInfo($"Disconnected from {_connectionString}");
		}

		public async Task ConnectAsync()
		{
			await _logger.LogInfo($"Connecting to {_connectionString}...");
			await _engine.ConnectAsync();
			await _logger.LogInfo($"Connected to {_connectionString}");
		}

		public async Task<bool> SendAsync(CalculationResultData resultData)
		{
			if (resultData.Status != CalculationStatus.Successful)
			{
				await _logger.LogError($"The result was not successful ({resultData.Status}), will not send SQL request");
				return false;
			}

			try
			{
				await _logger.LogInfo($"Sending SQL request to {_connectionString}...");

				await _engine.ConnectAsync();

				var affectedRowsCount = await _engine.SendAsync(resultData);
				await _logger.LogInfo($"Sent SQL request to {_connectionString}, {affectedRowsCount} rows affected");

				return true;
			}
			catch (Exception ex)
			{
				await _logger.LogException($"Failed to send an SQL request ({_connectionString})", ex);
				return false;
			}
		}
	}
}
