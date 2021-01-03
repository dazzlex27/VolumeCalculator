using System;
using System.Collections.Concurrent;
using System.IO.Ports;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using GodSharp.SerialPort;
using Primitives.Logging;

namespace DeviceIntegration.IoCircuits
{
	internal class KeUsb24RCircuit : IIoCircuit
	{
		private readonly object _lock;
		
		private readonly byte[] _headerBytes;
		private readonly byte[] _footerBytes;

		private readonly ILogger _logger;
		private readonly string _port;

		private readonly GodSerialPort _serialPort;

		private readonly ConcurrentQueue<string> _messageQueue;

		public KeUsb24RCircuit(ILogger logger, string port)
		{
			_lock = new object();
			
			_headerBytes = Encoding.ASCII.GetBytes("$KE");
			_footerBytes = new byte[] { 0x0D, 0x0A };

			_messageQueue = new ConcurrentQueue<string>();

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
			Task.Run(() =>
			{
				try
				{
					var stateCode = state ? 1 : 0;
					WriteData($",REL,{relayNum},{stateCode}");
				}
				catch (Exception ex)
				{
					_logger.LogException($"Failed to toggle a relay {relayNum} to {state}", ex);
				}
			});
		}

		public int PollLine(int lineNum)
		{
			lock (_lock)
			{
				WriteData($",RID,{lineNum}");

				var startTime = DateTime.Now;

				while (DateTime.Now - startTime < TimeSpan.FromSeconds(3))
				{
					while (!_messageQueue.IsEmpty)
					{
						var success = _messageQueue.TryDequeue(out var message);
						if (!success)
							break;

						var messageTokens = message.Split(',');
						if (messageTokens.Length < 3)
							continue;

						if (messageTokens[0] != "#RID")
							continue;

						if (!int.TryParse(messageTokens[1], out var parsedLineNum))
							continue;

						if (parsedLineNum == lineNum)
							return int.Parse(messageTokens[2]);
					}
				}

				return -1;
			}
		}

		private void ReadMessage(byte[] messageBytes)
		{
			var data = Encoding.ASCII.GetString(messageBytes);

			_messageQueue.Enqueue(data);
		}
	}
}
