using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using DeviceIntegrations.Scales;
using GodSharp.SerialPort;
using Primitives.Logging;

namespace DeviceIntegration.Scales
{
	public class MassaKScales : IScales
	{
		public event Action<ScaleMeasurementData> MeasurementReady;

		private const int ErrorTimeOutMs = 500;
		private readonly byte[] _pollMessage;
		private readonly byte[] _resetMessage;

		private readonly ILogger _logger;
		private readonly string _port;
		private readonly int _pollingRateMs;
		private readonly CancellationTokenSource _tokenSource;
		private readonly GodSerialPort _serialPort;

		public MassaKScales(ILogger logger, string port, int pollingRateMs)
		{
			_logger = logger;
			_port = port;
			_pollingRateMs = pollingRateMs;
			_tokenSource = new CancellationTokenSource();

			_logger.LogInfo($"Starting MassaKScales on port {_port}...");

			_pollMessage = BitConverter.GetBytes(0x4A);
			_resetMessage = BitConverter.GetBytes(0x0E);

			_serialPort = new GodSerialPort(port, 4800, "even", 8, "1", "0");
			_serialPort.UseDataReceived(true, (sp, bytes) =>
			{
				ReadData(bytes);
			});

			Task.Run(async () =>
			{
				try
				{
					await PollScales();
				}
				catch (Exception ex)
				{
					_logger.LogException("Exception in MassaKScales polling loop", ex);
				}
			});
		}

		public void Dispose()
		{
			_logger.LogInfo($"Disposing MassaKScales on port {_port}...");

			_tokenSource.Cancel();
			_serialPort.Close();
		}

		public void ResetWeight()
		{
			_serialPort.Write(_resetMessage, 0, 1);
		}

		private async Task PollScales()
		{
			_serialPort.Open();

			while (!_tokenSource.IsCancellationRequested)
			{
				try
				{
					_serialPort.Write(_pollMessage, 0, 1);

					await Task.Delay(_pollingRateMs);
				}
				catch (Exception ex)
				{
					_logger.LogException("Failed to poll data from MassaKScales", ex);
					await Task.Delay(ErrorTimeOutMs);
				}
			}
		}

		private void ReadData(IReadOnlyList<byte> messageBytes)
		{
			try
			{
				if (messageBytes.Count % 5 != 0)
					throw new InvalidDataException($"Incoming data was expected to be 5 bytes long, but was {messageBytes.Count}");

				var status = GetStatusFromMessage(messageBytes);
				var totalWeight = GetWeightFromMessage(messageBytes);
				var scaleMeasurementData = new ScaleMeasurementData(status, totalWeight);

				MeasurementReady?.Invoke(scaleMeasurementData);
			}
			catch (Exception ex)
			{
				_logger.LogException($"Failed to read data from MassaKScales on serial port {_port}", ex);
			}
		}

		private static MeasurementStatus GetStatusFromMessage(IReadOnlyList<byte> messageBytes)
		{
			if (messageBytes == null || messageBytes.Count < 1)
				return MeasurementStatus.NotSet;

			switch (messageBytes[0])
			{
				case 192:
					return MeasurementStatus.Ready;
				case 128:
					return MeasurementStatus.Measured;
				case 0:
					return MeasurementStatus.Measuring;
				default:
					return MeasurementStatus.Invalid;
			}
		}

		private static double GetWeightFromMessage(IReadOnlyList<byte> messageBytes)
		{
			var multipler = 1.0;
			var lastBitActive = (messageBytes[4] & (1 << 7)) != 0;
			if (lastBitActive)
				multipler *= -1;

			switch (messageBytes[1])
			{
				case 0:
					multipler *= 0.001;
					break;
				case 1:
					multipler *= 0.0001;
					break;
				case 4:
					multipler *= 0.01;
					break;
				case 5:
					multipler *= 0.1;
					break;
				default:
					multipler *= -1;
					break;
			}

			var rawWeight = messageBytes[2] + messageBytes[3] + messageBytes[4];
			if (lastBitActive)
				rawWeight -= 128;

			return rawWeight * multipler;
		}
	}
}