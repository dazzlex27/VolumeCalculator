using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO.Ports;
using System.Linq;
using System.Text;
using GodSharp.SerialPort;
using Primitives.Logging;

namespace DeviceIntegration.Scales
{
	internal class CasMScales : IScales
	{
		public event Action<ScaleMeasurementData> MeasurementReady;

		private readonly ILogger _logger;
		private readonly string _port;
		private readonly GodSerialPort _serialPort;

		public CasMScales(ILogger logger, string port)
		{
			_logger = logger;
			_port = port;

			_serialPort = new GodSerialPort(port, 9600, Parity.None, 8, StopBits.One);
			_serialPort.UseDataReceived(true, (sp, bytes) =>
			{
				ReadMessage(bytes);
			});

			_serialPort.Open();
		}

		public void Dispose()
		{
			_logger.LogInfo($"Disposing CasMScales on port {_port}...");

			_serialPort.Close();
		}

		public void ResetWeight()
		{
			_logger.LogError("CasMScales: ResetWeight() is not implemented");
		}

		public void ReadMessage(byte[] messageBytes)
		{
			var status = GetStatusFromMessage(messageBytes);
			var signMultipler = GetSignMultiplierFromMessage(messageBytes);
			var weight = GetWeightFromMessage(messageBytes);
			if (weight < 0.001)
			{
				status = MeasurementStatus.Ready;
				weight = 0;
			}

			var measurementData = new ScaleMeasurementData(status, weight * signMultipler);
			MeasurementReady?.Invoke(measurementData);
		}

		private static MeasurementStatus GetStatusFromMessage(IReadOnlyList<byte> messageBytes)
		{
			if (messageBytes == null || messageBytes.Count < 3)
				return MeasurementStatus.NotSet;

			switch (messageBytes[2])
			{
				case 70:
					return MeasurementStatus.Ready;
				case 83:
					return MeasurementStatus.Measured;
				case 85:
					return MeasurementStatus.Measuring;
				default:
					return MeasurementStatus.Invalid;
			}
		}

		private int GetSignMultiplierFromMessage(IReadOnlyList<byte> messageBytes)
		{
			switch (messageBytes[3])
			{
				case 32:
					return 1;
				case 45:
					return -1;
				case 70:
					return -1000000;
				default:
				{
					_logger.LogError("CasMScales: failed to parse multiplier");
					return 0;
				}
			}
		}

		private static double GetWeightFromMessage(IReadOnlyCollection<byte> messageBytes)
		{
			if (messageBytes == null || messageBytes.Count < 12)
				throw new ArgumentException("MasC scales: message is too short");

			var weightArray = messageBytes.Skip(4).Take(6).ToArray();
			var weightString = Encoding.ASCII.GetString(weightArray);
			var rawWeight = double.Parse(weightString, CultureInfo.InvariantCulture);

			var unitsArray = messageBytes.Skip(10).Take(2).ToArray();
			var unitsString = Encoding.ASCII.GetString(unitsArray);

			var multiplier = 1.0;

			switch (unitsString)
			{
				case "kg":
					break;
				case "lb":
					multiplier = 0.45359237;
					break;
			}

			return rawWeight * multiplier;
		}
	}
}