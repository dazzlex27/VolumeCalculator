using System;
using System.Threading;
using LibUsbDotNet;
using Primitives.Logging;

namespace DeviceIntegration.RangeMeters
{
	internal class FakeRangeMeter : IRangeMeter
	{
		private const int DefaultSubtractionValueMm = 140;
		private const int Vid = 1155;
		private const int Pid = 22352;

		private readonly ILogger _logger;

		private UsbDevice _teslaM70;
		private UsbEndpointWriter _writeEndpoint;
		private UsbEndpointReader _readEnpoint;

		private long _lastDistance;
		private int _subtractionValueMm;

		public FakeRangeMeter(ILogger logger)
		{
			_logger = logger;
			logger.LogInfo("Creating a fake range meter...");
			var deviceOpen = OpenDevice();
			if (!deviceOpen)
				throw new ApplicationException("Failed to open a fake range meter!");
			_lastDistance = 0;
			_subtractionValueMm = 0;
		}

		public void Dispose()
		{
			_teslaM70?.Close();
		}

		public void SetSubtractionValueMm(int value)
		{
			_subtractionValueMm = value;
		}

		public long GetReading()
		{
			SendReadCommand();
			Thread.Sleep(500);
			ReadCurrentRecord();

			var totalSubtractionValue = DefaultSubtractionValueMm + _subtractionValueMm;
			var lastDistanceMm = _lastDistance / 10 - totalSubtractionValue;
			_lastDistance = 0;
			return lastDistanceMm;
		}

		public void ToggleLaser(bool enable)
		{
			if (!enable)
				return;

			SendClearCommand();
			SendReadCommand();
		}

		private bool OpenDevice()
		{
			return true;
		}

		private bool SendString(string cmd)
		{
			if (cmd == "ATK009#")
				_lastDistance = 0;
			else
				_lastDistance = 12500;

			return true;
		}

		private void ReadCurrentRecord()
		{
			SendString("ATD001#");
		}

		private void SendClearCommand()
		{
			SendString("ATK009#");
		}

		private void SendReadCommand()
		{
			SendString("ATK001#");
		}

		private void ReadCurrentScreenRecord()
		{
			SendString("ATI001#");
		}
	}
}