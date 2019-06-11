using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Ports;
using System.Threading;
using System.Threading.Tasks;

namespace ComTestApp
{
	public class MassaKScales
	{
		public event Action<ScaleMeasurementData> MeasurementReady;

		private const int ErrorTimeOutMs = 500;
		private const int PollMessage = 0x4A;
		private const int ResetMessage = 0x0E;

		private readonly int _pollingRateMs;
		private readonly CancellationTokenSource _tokenSource;
		private readonly SerialPort _serialPort;

		public MassaKScales(string port, int pollingRateMs)
		{
			_pollingRateMs = pollingRateMs;
			_tokenSource = new CancellationTokenSource();

			_serialPort = new SerialPort(port)
			{
				BaudRate = 4800,
				Parity = Parity.Even,
				StopBits = StopBits.One,
				DataBits = 8,
				Handshake = Handshake.None
			};
			_serialPort.DataReceived += OnDataReceived;

			Task.Run(async () =>
			{
				try
				{
					await PollScales();
				}
				catch (Exception ex)
				{
					Console.WriteLine($"Poll task failed: {ex}");
				}
			});
		}

		public void Dispose()
		{
			_tokenSource.Cancel();
			_serialPort.Dispose();
		}

		public void ResetWeight()
		{
			var messageBytes = BitConverter.GetBytes(ResetMessage);
			_serialPort.Write(messageBytes, 0, 2);
		}

		private async Task PollScales()
		{
			_serialPort.Open();

			while (!_tokenSource.IsCancellationRequested)
			{
				try
				{
					var messageBytes = BitConverter.GetBytes(PollMessage);
					_serialPort.Write(messageBytes, 0, 2);

					await Task.Delay(_pollingRateMs);
				}
				catch (Exception ex)
				{
					Console.WriteLine($"Failed to poll scales: {ex}");
					await Task.Delay(ErrorTimeOutMs);
				}
			}
		}

		private void OnDataReceived(object sender, SerialDataReceivedEventArgs e)
		{
			try
			{
				var serialPort = (SerialPort)sender;

				var messageLength = serialPort.BytesToRead;
				var messageBytes = new byte[messageLength];
				serialPort.Read(messageBytes, 0, messageLength);

				var bytesToPrint = string.Join(" ", messageBytes);
				Console.WriteLine(bytesToPrint);

				if (messageBytes.Length % 5 != 0)
					throw new InvalidDataException($"Incoming data was expected to be 5 bytes long, but was {messageBytes.Length}");

				var status = GetStatusFromMessage(messageBytes);
				var totalWeight = GetWeightFromMessage(messageBytes);
				var scaleMeasurementData = new ScaleMeasurementData(status, totalWeight);

				MeasurementReady?.Invoke(scaleMeasurementData);
			}
			catch (Exception ex)
			{
				Console.WriteLine($"Failed to read data from the scales {ex}");
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

			var rawWeight = messageBytes[2] + messageBytes[3] * 256 + (char)messageBytes[4] * 256 * 256;
			if (lastBitActive)
				rawWeight *=-1;

			return rawWeight * multipler;
		}
	}
}