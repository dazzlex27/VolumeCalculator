using System;
using System.IO.Ports;
using System.Text;
using GodSharp.SerialPort;

namespace ComTestApp
{
	internal class Ci2001AScales : IScales
	{
		public event Action<ScaleMeasurementData> MeasurementReady;

		private readonly GodSerialPort _serialPort;

		public Ci2001AScales(string port)
		{
			_serialPort = new GodSerialPort(port, 19200, Parity.None, 8, StopBits.One, Handshake.RequestToSend);
			_serialPort.UseDataReceived(true, (sp, bytes) =>
			{
				ReadMessage(bytes);
			});

			_serialPort.Open();
		}

		public void Dispose()
		{
			_serialPort.Close();
		}

		public void ResetWeight()
		{
		}

		public void ReadMessage(byte[] messageBytes)
		{
			if (messageBytes.Length < 20)
				throw new ArgumentException(
					$"CI2001A scales: the message is too short (expected at least 20 bytes, but received {messageBytes.Length}");

			var messageString = Encoding.ASCII.GetString(messageBytes);
			var messageTokens = messageString.Split(',');
			if (messageTokens.Length < 4)
				throw new ArgumentException(
					$"CI2001A scales: the message does not contain enough commas, the number of commas was {messageTokens.Length}");

			var status = GetStatusFromMessage(messageTokens[0]);
			var weight = GetWeightFromMessage(messageTokens[3]);
			if (weight < 0.001)
			{
				status = MeasurementStatus.Ready;
				weight = 0;
			}

			var measurementData = new ScaleMeasurementData(status, weight);
			MeasurementReady?.Invoke(measurementData);
		}

		private double GetWeightFromMessage(string messageToken)
		{
			var trimmedMessageToken = messageToken.Replace("\r\n", "").Trim();
			var weightMessageTokens = trimmedMessageToken.Split(new[] {' '}, StringSplitOptions.RemoveEmptyEntries);

			var multiplier = 1.0;
			var weight = double.Parse(weightMessageTokens[0]);

			switch (weightMessageTokens[1])
			{
				case "kg":
					break;
				case "lb":
					multiplier = 0.45359237;
					break;
			}

			return weight * multiplier;
		}

		private MeasurementStatus GetStatusFromMessage(string firstMessageToken)
		{
			switch (firstMessageToken)
			{
				case "ST":
					return MeasurementStatus.Measured;
				case "US":
					return MeasurementStatus.Measuring;
				case "OL":
					return MeasurementStatus.Overload;
				default:
					return MeasurementStatus.Invalid;
			}
		}
	}
}
