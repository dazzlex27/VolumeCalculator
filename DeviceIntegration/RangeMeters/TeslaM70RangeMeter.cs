using System;
using System.Text;
using System.Threading;
using LibUsbDotNet;
using LibUsbDotNet.Main;
using Primitives.Logging;

namespace DeviceIntegration.RangeMeters
{
	internal class TeslaM70RangeMeter : IRangeMeter
	{
		private const int DefaultSubtractionValueMm = 140;
		private const int Vid = 1155;
		private const int Pid = 22352;

		private readonly object _lock;

		private readonly ILogger _logger;

		private UsbDevice _teslaM70;
		private UsbEndpointWriter _writeEndpoint;
		private UsbEndpointReader _readEnpoint;

		private long _lastDistance;
		private int _subtractionValueMm;

		public TeslaM70RangeMeter(ILogger logger)
		{
			_lock = new object();
			
			_logger = logger;
			logger.LogInfo("Creating a TeslaM70 range meter...");
			var deviceOpen = OpenDevice();
			if (!deviceOpen)
				throw new ApplicationException("Failed to open TeslaM70 range meter!");
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
			lock (_lock)
			{
				SendReadCommand();
				Thread.Sleep(500);
				ReadCurrentRecord();

				var totalSubtractionValue = DefaultSubtractionValueMm + _subtractionValueMm;
				var lastDistanceMm = _lastDistance / 10 - totalSubtractionValue;
				_lastDistance = 0;
				return lastDistanceMm;
			}
		}

		public void ToggleLaser(bool enable)
		{
			lock (_lock)
			{
				if (!enable)
					return;

				SendClearCommand();
				SendReadCommand();
			}
		}

		private bool OpenDevice()
		{
			var usbFinder = new UsbDeviceFinder(Vid, Pid);
			_teslaM70 = UsbDevice.OpenUsbDevice(usbFinder);
			if (_teslaM70 == null)
			{
				_logger.LogError("Failed to create a device");
				return false;
			}

			_teslaM70.Open();
			if (_teslaM70 is IUsbDevice usbDevice)
			{
				usbDevice.SetConfiguration(1);
				usbDevice.ClaimInterface(0);
			}

			_writeEndpoint = _teslaM70.OpenEndpointWriter(WriteEndpointID.Ep01);
			_readEnpoint = _teslaM70.OpenEndpointReader(ReadEndpointID.Ep02);

			return true;
		}

		private bool SendString(string cmd)
		{
			var bytes = Encoding.Default.GetBytes(cmd);
			var array = new byte[1024];
			var errorCode = _writeEndpoint.SubmitAsyncTransfer(bytes, 0, bytes.Length, 1000, out _);
			errorCode = _readEnpoint.Read(array, 3000, out _);
			if (errorCode != ErrorCode.None || array[2] != 68)
				return errorCode == ErrorCode.None;

			_lastDistance = ((array[3] << 24) | (array[4] << 16) | (array[5] << 8) | array[6]);
			//lastAngleX = ((array[7] << 24) | (array[8] << 16) | (array[9] << 8) | array[10]);
			//lastAngleY = ((array[11] << 24) | (array[12] << 16) | (array[13] << 8) | array[14]);

			return errorCode == ErrorCode.None;
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