using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Ports;
using System.Linq;
using System.Text;
using GodSharp.SerialPort;
using Primitives.Logging;

namespace DeviceIntegration.Scales
{
	internal class CasMScales : IScales
	{
		private static readonly TimeSpan StabilizationTimeDelta = TimeSpan.FromMilliseconds(300);

		public event Action<ScaleMeasurementData> MeasurementReady;

		private readonly ILogger _logger;
		private readonly string _port;
		private readonly int _minWeight;

		private readonly GodSerialPort _serialPort;

		private readonly bool _deepLoggingOn;

		private bool _paused;

		private double _lastWeightGr;
		private MeasurementStatus _lastMeasurementStatus;
		private DateTime _lastWeightChangedTime;

		public CasMScales(ILogger logger, string port, int minWeight)
		{
			_logger = logger;
			_port = port;
			_minWeight = minWeight;

			_logger.LogInfo($"Starting CasMScales on port {_port}...");

			_deepLoggingOn = File.Exists("SCALESLOGGING");

			_lastWeightGr = -1;
			_lastMeasurementStatus = MeasurementStatus.NotSet;
			_lastWeightChangedTime = DateTime.MinValue;

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
			if (_lastWeightGr < _minWeight)
				return;

			_lastWeightGr = 0;
			_lastMeasurementStatus = MeasurementStatus.Ready;
		}

		public void TogglePause(bool pause)
		{
			_paused = pause;
		}

		private void ReadMessage(byte[] messageBytes)
		{
			if (_paused)
				return;

			if (messageBytes.Length < 4)
				return;

			if (_deepLoggingOn)
			{
				var rawMessage = string.Join(" ", messageBytes);
				var sourceString = Encoding.ASCII.GetString(messageBytes);
				var parsedDataString = $"status={_lastMeasurementStatus} last={_lastWeightGr}";
				_logger.LogInfo(
					$"{DateTime.Now.ToLocalTime()}: CasMScales ({_port}) rawMessage={rawMessage}, source message={sourceString}, {parsedDataString}");
			}

			var isOverLoaded = messageBytes[3] == 70; // 70 is "F" in ASCII
			if (isOverLoaded)
			{
				_lastMeasurementStatus = MeasurementStatus.Overload;
				_lastWeightGr = double.NaN;
			}
			else
			{
				var currentWeight = GetWeightFromMessage(messageBytes);
				if (currentWeight < _minWeight)
				{
					_lastMeasurementStatus = MeasurementStatus.Ready;
					_lastWeightGr = 0;
				}
				else
				{
					if (Math.Abs(currentWeight - _lastWeightGr) > 2) // different
					{
						_lastMeasurementStatus = MeasurementStatus.Measuring;
						_lastWeightGr = currentWeight;
						_lastWeightChangedTime = DateTime.Now;
					}
					else
					{
						var currentTime = DateTime.Now;
						if (currentTime - _lastWeightChangedTime > StabilizationTimeDelta)
						{
							_lastMeasurementStatus = MeasurementStatus.Measured;
							_lastWeightGr = currentWeight;
							_lastWeightChangedTime = currentTime;
						}
					}
				}
			}

			var measurementData = new ScaleMeasurementData(_lastMeasurementStatus, _lastWeightGr);
			MeasurementReady?.Invoke(measurementData);
		}

		private int GetWeightFromMessage(IReadOnlyCollection<byte> messageBytes)
		{
			if (messageBytes == null || messageBytes.Count < 12)
				throw new ArgumentException("MasC scales: message is too short");

			var weightArray = messageBytes.Skip(4).Take(6).ToArray();
			var weightString = Encoding.ASCII.GetString(weightArray);
			var weight = double.Parse(weightString, CultureInfo.InvariantCulture);

			var unitsArray = messageBytes.Skip(10).Take(2).ToArray();
			var unitsString = Encoding.ASCII.GetString(unitsArray);

			double multiplier;

			switch (unitsString)
			{
				case "kg":
					multiplier = 1000;
					break;
				case "lb":
					multiplier = 453.59237;
					break;
				case " g":
					multiplier = 1;
					break;
				case "oz":
					multiplier = 28.3495;
					break;
				default:
					_logger.LogError($"CasM scales: failed to find weight multiplier \"{unitsString}\"");
					multiplier = 0;
					break;
			}

			return (int)Math.Floor(weight * multiplier);
		}
	}
}
