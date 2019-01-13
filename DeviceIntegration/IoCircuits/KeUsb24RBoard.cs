using System;
using System.IO.Ports;
using System.Linq;
using System.Text;
using Primitives.Logging;

namespace DeviceIntegrations.IoCircuits
{
	public class KeUsb24RBoard : IIoCircuit
	{
		private readonly byte[] _headerBytes;
		private readonly byte[] _footerBytes;

		private readonly ILogger _logger;
		private readonly string _port;

		private readonly SerialPort _serialPort;

		public KeUsb24RBoard(ILogger logger, string port)
		{
			logger.LogInfo($"Starting KeUsb24RBoard on port 1 {_port}...");
			_headerBytes = Encoding.ASCII.GetBytes("$KE");
			_footerBytes = new byte[] { 0x0D, 0x0A };

			logger.LogInfo($"Starting KeUsb24RBoard on port 2 {_port}...");

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

			//_serialPort.DataReceived += OnDataReceived;

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

		//private void OnDataReceived(object sender, SerialDataReceivedEventArgs e)
		//{
		//	try
		//	{
		//		var serialPort = (SerialPort)sender;

		//		var hexString = serialPort.ReadExisting();

		//		var bytes = Encoding.ASCII.GetBytes(hexString);
		//		var result = string.Join(" ", bytes);
		//		Console.WriteLine(result);
		//	}
		//	catch (Exception ex)
		//	{
		//		_logger.LogException("Failed to read data from KeUsb24RBoard", ex);
		//	}
		//}
	}
}
