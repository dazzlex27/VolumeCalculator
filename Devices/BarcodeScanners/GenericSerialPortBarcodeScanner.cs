﻿using System;
using System.IO.Ports;
using DeviceIntegration;
using Primitives.Logging;

namespace BarcodeScanners
{
	internal class GenericSerialPortBarcodeScanner : IBarcodeScanner
	{
		public event Action<string> CharSequenceFormed;

		private readonly ILogger _logger;
		private readonly SerialPort _serialPort;
		private readonly string _port;

		private bool _paused;

		public GenericSerialPortBarcodeScanner(ILogger logger, string port)
		{
			_logger = logger;
			_port = port;

			_logger.LogInfo($"Starting a generic serial port scanner on port {_port}...");

			_serialPort = new SerialPort(port)
			{
				BaudRate = 9600,
				Parity = Parity.None,
				StopBits = StopBits.One,
				DataBits = 8,
				Handshake = Handshake.None
			};

			_serialPort.DataReceived += OnDataReceived;

			_serialPort.Open();
		}

		public void Dispose()
		{
			_logger.LogInfo($"Disposing a generic serial port scanner on port {_port}...");
			_serialPort.Close();
			_serialPort.Dispose();
		}

		public void TogglePause(bool pause)
		{
			_paused = pause;
		}

		private void OnDataReceived(object sender, SerialDataReceivedEventArgs e)
		{
			if (_paused)
				return;

			try
			{
				var serialPort = (SerialPort)sender;
				var indata = serialPort.ReadExisting();
				var trimmedData = indata.Replace(Environment.NewLine, "").Replace(" ", "").Replace("\t", "");

				CharSequenceFormed?.Invoke(trimmedData);
			}
			catch (Exception ex)
			{
				_logger.LogException($"Failed to read data from serial port {_port}", ex);
			}
		}
	}
}
