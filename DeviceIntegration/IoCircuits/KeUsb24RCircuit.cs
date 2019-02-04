using System;
using System.IO.Ports;
using System.Linq;
using System.Text;
using DeviceIntegrations.IoCircuits;
using Primitives.Logging;

namespace DeviceIntegration.IoCircuits
{
	public class KeUsb24RCircuit : IIoCircuit
	{
		private readonly byte[] _headerBytes;
		private readonly byte[] _footerBytes;

		private readonly ILogger _logger;
		private readonly string _port;

		private readonly SerialPort _serialPort;

		public KeUsb24RCircuit(ILogger logger, string port)
		{
			_headerBytes = Encoding.ASCII.GetBytes("$KE");
			_footerBytes = new byte[] { 0x0D, 0x0A };

			_logger = logger;
			_port = port;

			_logger.LogInfo($"Starting KeUsb24RBoard on port {_port}...");

			_serialPort = new SerialPort(port)
			{
				BaudRate = 4800,
				Parity = Parity.Even,
				StopBits = StopBits.One,
				DataBits = 8,
				Handshake = Handshake.None
			};

			_serialPort.Open();
		}

		public void Dispose()
		{
			_serialPort.Close();
			_serialPort.Dispose();
		}

		public void WriteData(string data)
		{
			try
			{
				var bytes = Encoding.ASCII.GetBytes(data);

				var messageBytes = _headerBytes.Concat(bytes).Concat(_footerBytes).ToArray();

				_serialPort.Write(messageBytes, 0, messageBytes.Length);
			}
			catch (Exception ex)
			{
				_logger.LogException($"Failed to write data to KeUsb24RBoard port {_port}", ex);
			}
		}

		public void ToggleRelay(int relayNum, bool state)
		{
			var stateCode = state ? 1 : 0;
			WriteData($",REL,{relayNum.ToString()},{stateCode}");
		}
	}
}
