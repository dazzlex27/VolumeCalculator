using System;
using System.IO.Ports;
using System.Linq;
using GodSharp.SerialPort;
using Primitives.Logging;

namespace DeviceIntegration.RangeMeters
{
	internal class CustomRangeMeter : IRangeMeter
	{
		private readonly ILogger _logger;
		private readonly string _port;
		private readonly GodSerialPort _serialPort;

		public CustomRangeMeter(ILogger logger, string port)
		{
			_logger = logger;
			_port = port;

			_serialPort = new GodSerialPort(port, 115200, Parity.None, 8, StopBits.One);
			_serialPort.Open();
		}

		public void Dispose()
		{
			_serialPort.Close();
		}

		public void ToggleLaser(bool enable)
		{
			var messageSymbol = (byte) (enable ? 49 : 48);
			var message = new[] {messageSymbol};
			_serialPort.Write(message);
		}

		public long GetReading()
		{
			try
			{
				const byte message = 2;
				_serialPort.Write(new[] {message});

				var readingsString = _serialPort.ReadExisting();
				string[] stringSeparators = {"\r\n"};
				var lines = readingsString.Split(stringSeparators, StringSplitOptions.RemoveEmptyEntries);
				return (int) lines.Select(int.Parse).Average();
			}
			catch (Exception ex)
			{
				_logger.LogException($"CustomRangeMeter: failed to get reading on port {_port}", ex);
				return -1;
			}
		}
	}
}