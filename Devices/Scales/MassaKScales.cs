using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Ports;
using System.Threading;
using System.Threading.Tasks;
using GodSharp.SerialPort;
using Primitives.Logging;

namespace DeviceIntegration.Scales
{
	internal class MassaKScales : IScales
	{
		public event Action<ScaleMeasurementData> MeasurementReady;

		private readonly byte[] _resetMessage;

		private readonly ILogger _logger;
		private readonly string _port;
		private readonly int _minWeight;

		private readonly CancellationTokenSource _tokenSource;
		private readonly GodSerialPort _serialPort;

		private readonly bool _deepLoggingOn;

		private bool _paused;

		public MassaKScales(ILogger logger, string port, int minWeight)
		{
			_logger = logger;
			_port = port;
			_minWeight = minWeight;

			_tokenSource = new CancellationTokenSource();

			_logger.LogInfo($"Starting MassaKScales on port {_port}...");

			_deepLoggingOn = File.Exists("SCALESLOGGING");

			_resetMessage = BitConverter.GetBytes(0x0E);

			_serialPort = new GodSerialPort(port, 4800, Parity.Even, 8, StopBits.One);
			_serialPort.UseDataReceived(true, (sp, bytes) =>
			{
				try
				{
					ReadMessage(bytes);
				}
				catch (Exception ex)
				{
					_logger.LogException("Failed to read MassaKScales message", ex);
				}
			});

			_serialPort.Open();

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

		public void TogglePause(bool pause)
		{
			_paused = pause;
		}

		private async Task PollScales()
		{
			const int pollingRateMs = 300;
			const int errorTimeOutMs = 500;

			var pollMessage = BitConverter.GetBytes(0x4A);

			while (!_tokenSource.IsCancellationRequested)
			{
				try
				{
					_serialPort.Write(pollMessage, 0, 1);

					await Task.Delay(pollingRateMs);
				}
				catch (Exception ex)
				{
					_logger.LogException("Failed to poll data from MassaKScales", ex);
					await Task.Delay(errorTimeOutMs);
				}
			}
		}

		private void ReadMessage(IReadOnlyList<byte> messageBytes)
		{
			if (_paused)
				return;

			if (_deepLoggingOn)
				_logger.LogInfo($"MassaKScales ({_port}) message: {string.Join(" ", messageBytes)}");

			try
			{
				if (messageBytes.Count < 5)
					return;

				var status = GetStatusFromMessage(messageBytes);
				if (status == MeasurementStatus.Invalid)
					return;

				var weight = GetWeightFromMessage(messageBytes);
				if (weight < _minWeight)
				{
					status = MeasurementStatus.Ready;
					weight = 0;
				}

				if (_deepLoggingOn)
					_logger.LogInfo($"MassaKScales ({_port}) weight data: {status} {weight}");

				var measurementData = new ScaleMeasurementData(status, weight);
				MeasurementReady?.Invoke(measurementData);
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

		private int GetWeightFromMessage(IReadOnlyList<byte> messageBytes)
		{
			var weight = messageBytes[2] + messageBytes[3] * 256 + (char)messageBytes[4] * 256 * 256;

			var multiplier = 1.0;
			var lastBitActive = (messageBytes[4] & (1 << 7)) != 0;
			if (lastBitActive)
				multiplier *= -1;

			switch (messageBytes[1])
			{
				case 0:
					multiplier *= 0.001;
					break;
				case 1:
					multiplier *= 0.0001;
					break;
				case 4:
					multiplier *= 0.01;
					break;
				case 5:
					multiplier *= 0.1;
					break;
				default:
					multiplier = 0;
					_logger.LogError($"Failed to read scale multiplier data, the value was {messageBytes[1]}");
					break;
			}

			return (int)Math.Floor(weight * multiplier * 1000);
		}
	}
}
