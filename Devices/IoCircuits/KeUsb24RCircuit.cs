using System;
using System.Collections.Concurrent;
using System.IO.Ports;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using DeviceIntegration.IoCircuits;
using GodSharp.SerialPort;
using Primitives.Logging;

namespace IoCircuits
{
	internal class KeUsb24RCircuit : IIoCircuit
	{
		private readonly byte[] _headerBytes;
		private readonly byte[] _footerBytes;

		private readonly ILogger _logger;
		private readonly string _port;

		private readonly GodSerialPort _serialPort;

		private readonly ConcurrentDictionary<int, LineRequestStatus> _lineStates;
		private readonly ConcurrentDictionary<int, int> _lastLineValues;

		public KeUsb24RCircuit(ILogger logger, string port)
		{
			_logger = logger;
			_port = port;

			_logger.LogInfo($"Starting KeUsb24RBoard on port {_port}...");

			_headerBytes = Encoding.ASCII.GetBytes("$KE");
			_footerBytes = new byte[] { 0x0D, 0x0A };

			_lineStates = new ConcurrentDictionary<int, LineRequestStatus>();
			_lastLineValues = new ConcurrentDictionary<int, int>();
			for (var i = 0; i < 32; i++)
			{
				_lineStates.TryAdd(i, LineRequestStatus.None);
				_lastLineValues.TryAdd(i, -1);
			}

			_serialPort = new GodSerialPort(port, 4800, Parity.Even, 8, StopBits.One, Handshake.None);
			_serialPort.UseDataReceived(true, (sp, bytes) =>
			{
				ReadMessage(bytes);
			});

			_serialPort.Open();

			WriteData(",AFR,0"); // disable spam from analog ports
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
			var startTime = DateTime.Now;

			_lineStates[lineNum] = LineRequestStatus.UpdateRequested;

			while (DateTime.Now - startTime < TimeSpan.FromSeconds(3))
			{
				switch (_lineStates[lineNum])
				{
					case LineRequestStatus.UpdateRequested:
						WriteData($",RID,{lineNum}");
						Thread.Sleep(10);
						continue;
					case LineRequestStatus.UpdateReceived:
						_lineStates[lineNum] = LineRequestStatus.None;
						return _lastLineValues[lineNum];
				}
			}

			return int.MinValue;
		}

		private void ReadMessage(byte[] messageBytes)
		{
			var data = Encoding.ASCII.GetString(messageBytes);

			var allMessages = data.Split(new[] { Environment.NewLine },
				StringSplitOptions.RemoveEmptyEntries);

			foreach (var message in allMessages)
			{
				var messageTokens = message.Split(',');

				if (messageTokens[0] != "#RID")
					continue;

				if (!int.TryParse(messageTokens[1], out var parsedLineNum))
					continue;

				var linesRequested = _lineStates
					.Where(i => i.Value == LineRequestStatus.UpdateRequested)
					.Select(i => i.Key).ToList();

				if (!linesRequested.Contains(parsedLineNum))
					continue;

				if (!int.TryParse(messageTokens[2], out var lineValue))
					continue;

				_lastLineValues[parsedLineNum] = lineValue;
				_lineStates[parsedLineNum] = LineRequestStatus.UpdateReceived;
			}
		}
	}
}