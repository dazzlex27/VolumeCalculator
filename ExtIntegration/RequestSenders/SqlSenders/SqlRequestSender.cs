using System;
using Primitives;
using Primitives.Logging;
using Primitives.Settings.Integration;

namespace ExtIntegration.RequestSenders.SqlSenders
{
	public class SqlRequestSender : IRequestSender
	{
		private readonly ILogger _logger;

		private ISqlRequestSenderEngine _engine;
		private readonly string _connectionString;

		public SqlRequestSender(ILogger logger, SqlRequestSettings requestSettings)
		{
			_logger = logger;

			var insertionSqlRequest = $"INSERT INTO {requestSettings.TableName} (WEIGHT, LENGTH, WIDTH, HEIGHT, BARCODE) VALUES (@weight, @length, @width, @height, @barcode);";

			_engine = SqlRequestEngineFactory.CreateEngine(requestSettings, insertionSqlRequest);
			_connectionString = _engine.GetConnectionString();
		}

		public void Dispose()
		{
			_logger.LogInfo($"Disconnecting from {_connectionString}...");
			_engine.Disconnect();
			_logger.LogInfo($"Disconnected from {_connectionString}");
		}

		public void Connect()
		{
			_logger.LogInfo($"Connecting to {_connectionString}...");
			_engine.Connect();
			_logger.LogInfo($"Connected to {_connectionString}");
		}

		public bool Send(CalculationResultData resultData)
		{
			if (resultData.Status != CalculationStatus.Sucessful)
			{
				_logger.LogError($"The result was not successful ({resultData.Status}), will not send SQL request");
				return false;
			}

			try
			{
				_logger.LogInfo($"Sending SQL request to {_connectionString}...");

				_engine.Connect();

				var result = resultData.Result;

				var affectedRowsCount = _engine.Send(resultData);
				_logger.LogInfo($"Sent SQL request to {_connectionString}, {affectedRowsCount} rows affected");

				return true;
			}
			catch (Exception ex)
			{
				_logger.LogException($"Failed to send an SQL request ({_connectionString})", ex);
				return false;
			}
		}
	}
}