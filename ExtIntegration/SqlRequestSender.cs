﻿using System;
using System.Data.SqlClient;
using Primitives;
using Primitives.Logging;
using Primitives.Settings;

namespace ExtIntegration
{
	public class SqlRequestSender : IRequestSender
	{
		private readonly ILogger _logger;
		private readonly SqlConnection _connection;
		private readonly string _insertionSqlRequest;

		public SqlRequestSender(ILogger logger, SqlRequestSettings requestSettings)
		{
			_logger = logger;

			var builder = new SqlConnectionStringBuilder
			{
				DataSource = requestSettings.HostName,
				UserID = requestSettings.Username,
				Password = requestSettings.Password,
				InitialCatalog = requestSettings.DbName
			};

			_insertionSqlRequest = $"INSERT {requestSettings.TableName} (WEIGHT, LENGHT, WIDTH, HEIGHT, BARCODE) VALUES (@weight, @length, @width, @height, @barcode);";

			_connection = new SqlConnection(builder.ConnectionString);
		}

		public void Dispose()
		{
			Disconnect();
		}

		public void Connect()
		{
			_logger.LogInfo($"Connecting to {_connection.ConnectionString}...");
			_connection.Open();
			_logger.LogInfo($"Connected to {_connection.ConnectionString}");
		}

		public bool Send(CalculationResult result)
		{
			try
			{
				_logger.LogInfo($"Sending SQL request to {_connection.ConnectionString}...");

				using (var command = new SqlCommand(_insertionSqlRequest, _connection))
				{
					command.Parameters.AddWithValue("@weight", (int) (result.ObjectWeightKg * 1000));
					command.Parameters.AddWithValue("@length", result.ObjectLengthMm);
					command.Parameters.AddWithValue("@width", result.ObjectWidthMm);
					command.Parameters.AddWithValue("@height", result.ObjectHeightMm);
					command.Parameters.AddWithValue("@barcode", result.ObjectCode);
					var affectedRowsCount = command.ExecuteNonQuery();
					_logger.LogInfo($"Sent SQL request to {_connection.ConnectionString}, {affectedRowsCount} rows affected");
				}

				return true;
			}
			catch (Exception ex)
			{
				_logger.LogException($"Failed to send an SQL request ({_connection.ConnectionString})", ex);
				return false;
			}
		}

		public void Disconnect()
		{
			_logger.LogInfo($"Disconnecting from {_connection.ConnectionString}...");
			_connection.Open();
			_logger.LogInfo($"Disconnected from {_connection.ConnectionString}");
		}
	}
}