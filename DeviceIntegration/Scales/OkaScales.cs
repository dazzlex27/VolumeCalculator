using System;
using System.IO.Ports;
using System.Threading;
using System.Threading.Tasks;
using GodSharp.SerialPort;
using Primitives.Logging;

namespace DeviceIntegration.Scales
{
	internal class OkaScales : IScales
	{
		public event Action<ScaleMeasurementData> MeasurementReady;

		private readonly ILogger _logger;
		private readonly string _port;
		private readonly GodSerialPort _serialPort;
		private readonly CancellationTokenSource _tokenSource;

		public OkaScales(ILogger logger, string port)
		{
			_logger = logger;
			_port = port;
			_tokenSource = new CancellationTokenSource();

			_logger.LogInfo($"Creating OkaScales scales on port {port}...");

			_serialPort = new GodSerialPort(port, 4800, Parity.Even, 8, StopBits.Two);
			_serialPort.UseDataReceived(true, (sp, bytes) =>
			{
				try
				{
					ReadMessage(bytes);
				}
				catch (Exception ex)
				{
					_logger.LogException("Failed to read OkaScales message", ex);
				}
			});

			_serialPort.Open();

			var data = new byte[32];

			Task.Run(async () =>
			{
				try
				{
					await PollScales();
				}
				catch (Exception ex)
				{
					_logger.LogException("Exception in OkaScales polling loop", ex);
				}
			});
		}

		public void Dispose()
		{
			_logger.LogInfo($"Disposing OkaScales scales on port {_port}...");
			_serialPort.Close();
		}

		public void ResetWeight()
		{
		}

		private void ReadMessage(byte[] messageBytes)
		{
			Thread.Sleep(500);

			if (messageBytes.Length == 0)
				return;

			var weight = GetWeightFromMessage(messageBytes);
			var status = MeasurementStatus.Measured;
			if (weight < 1)
			{
				status = MeasurementStatus.Ready;
				weight = 0;
			}

			var measurementData = new ScaleMeasurementData(status, weight);
			MeasurementReady?.Invoke(measurementData);
		}

		private int GetWeightFromMessage(byte[] messageBytes)
		{
			try
			{
				var messageCopy = new byte[messageBytes.Length];

				Buffer.BlockCopy(messageBytes, 0, messageCopy, 0, messageBytes.Length);
				Array.Reverse(messageCopy);

				var joinedReversedWeightString = string.Join("", messageCopy);
				var trimmedString = joinedReversedWeightString.TrimStart(new char[] { '0' });
				if (trimmedString.Length == 0)
					trimmedString = "0";

				return int.Parse(trimmedString);
			}
			catch (Exception ex)
			{
				var messageBytesString = string.Join(" ", messageBytes);
				_logger.LogException($"Failed to parse OkaScalesMessage: {messageBytesString}", ex);

				return -1;
			}
		}

		private async Task PollScales()
		{
			const int pollingRateMs = 1000;
			const int errorTimeOutMs = 500;

			var pollWeightMessageArray = new byte[32];
			pollWeightMessageArray[31] = 3;

			while (!_tokenSource.IsCancellationRequested)
			{
				try
				{
					_serialPort.Write(pollWeightMessageArray);

					await Task.Delay(pollingRateMs);
				}
				catch (Exception ex)
				{
					_logger.LogException("Failed to poll data from OkaScales", ex);
					await Task.Delay(errorTimeOutMs);
				}
			}
		}
	}
}