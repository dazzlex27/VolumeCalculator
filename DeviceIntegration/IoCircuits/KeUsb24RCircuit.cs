using System;
using System.IO.Ports;
using System.Linq;
using System.Text;
using GodSharp.SerialPort;
using Primitives.Logging;

namespace DeviceIntegration.IoCircuits
{
	internal class KeUsb24RCircuit : IIoCircuit
	{
		private readonly byte[] _headerBytes;
		private readonly byte[] _footerBytes;

		private readonly ILogger _logger;
		private readonly string _port;

		private readonly GodSerialPort _serialPort;

		public KeUsb24RCircuit(ILogger logger, string port)
		{
			_headerBytes = Encoding.ASCII.GetBytes("$KE");
			_footerBytes = new byte[] { 0x0D, 0x0A };

			_logger = logger;
			_port = port;

			_logger.LogInfo($"Starting KeUsb24RBoard on port {_port}...");

			_serialPort = new GodSerialPort(port, 4800, Parity.Even, 8, StopBits.One, Handshake.None);
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
			WriteData($",REL,{relayNum},{stateCode}");
		}

		public void PollLine(int lineNum)
		{
			WriteData($",RID,{lineNum}");
		}

		private void ReadMessage(byte[] messageBytes)
		{
			var data = Encoding.ASCII.GetString(messageBytes);

			_logger.LogInfo(data);
		}
	}
}
