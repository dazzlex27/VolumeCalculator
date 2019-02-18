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
		private const int Vid = 1155;

		private const int Pid = 22352;

		private UsbDevice _teslaM70;

		private UsbEndpointWriter _writeEndpoint;

		private UsbEndpointReader _readEnpoint;

		private long _lastDistance;

		private bool _laserOn;

		public TeslaM70RangeMeter(ILogger logger, string port)
		{
			logger.LogInfo("Creating a TeslaM70 range meter...");
			var deviceOpen = OpenDevice();
			if (!deviceOpen)
				throw new ApplicationException("Failed to open TeslaM70 range meter!");
			_lastDistance = 0;
		}

		public void Dispose()
		{
			_teslaM70?.Close();
		}

		public long GetReading()
		{
			_laserOn = !_laserOn;

			SendReadCommand();
			Thread.Sleep(500);
			ReadCurrentRecord();

			var lastDistance = _lastDistance;
			_lastDistance = 0;
			_laserOn = false;
			return lastDistance;
		}

		public void ToggleLaser(bool enable)
		{
			if (enable && !_laserOn)
			{
				SendReadCommand();
				_laserOn = true;
			}
			//TurnOnLaser();
		}

		private bool OpenDevice()
		{
			var usbFinder = new UsbDeviceFinder(Vid, Pid);
			UsbRegDeviceList allDevices = UsbDevice.AllDevices;
			UsbRegDeviceList usbRegDeviceList = new UsbRegDeviceList();
			usbRegDeviceList = usbRegDeviceList.FindAll(usbFinder);
			_teslaM70 = UsbDevice.OpenUsbDevice(usbFinder);
			if (_teslaM70 == null)
				return false;

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
			byte[] bytes = Encoding.Default.GetBytes(cmd);
			byte[] array = new byte[1024];
			ErrorCode errorCode = _writeEndpoint.SubmitAsyncTransfer(bytes, 0, bytes.Length, 1000, out UsbTransfer _);
			errorCode = _readEnpoint.Read(array, 3000, out int _);
			if (errorCode != ErrorCode.None || array[2] != 68)
				return errorCode == ErrorCode.None;

			_lastDistance = ((array[3] << 24) | (array[4] << 16) | (array[5] << 8) | array[6]);
			//lastAngleX = ((array[7] << 24) | (array[8] << 16) | (array[9] << 8) | array[10]);
			//lastAngleY = ((array[11] << 24) | (array[12] << 16) | (array[13] << 8) | array[14]);

			return errorCode == ErrorCode.None;
		}

		private void SendClearCommand()
		{
			SendString("ATK009#");
		}

		private void SendReadCommand()
		{
			SendString("ATK001#");
		}

		private void ReadCurrentRecord()
		{
			SendString("ATD001#");
		}

		private void ReadCurrentScreenRecord()
		{
			SendString("ATI001#");
		}

		private void TurnOnLaser()
		{
			var bytes = Encoding.Default.GetBytes("ATI001#");
			byte[] array = new byte[1024];
			var errorCode = _writeEndpoint.SubmitAsyncTransfer(bytes, 0, bytes.Length, 1000, out _);
			var buffer = new byte[]
			{
				65,
				84,
				83,
				48,
				48,
				49,
				35
			};
			if (_writeEndpoint.Write(buffer, 3000, out int _) != ErrorCode.None)
				return;

			var setupPacket = new UsbSetupPacket(64, 1, 1, 0, 0);
			var buffer2 = new byte[100];
			var ok = _teslaM70.ControlTransfer(ref setupPacket, buffer2, 100, out _);
		}
	}
}