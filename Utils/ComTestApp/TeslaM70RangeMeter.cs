using System.Text;
using LibUsbDotNet;
using LibUsbDotNet.Main;

namespace ComTestApp
{
	internal class TeslaM70RangeMeter : IRangeMeter
	{
		private const int Vid = 1155;

		private const int Pid = 22352;

		private UsbDevice _teslaM70;

		public static UsbDeviceFinder usbFinder = new UsbDeviceFinder(Vid, Pid);

		private UsbEndpointWriter writeEndpoint;

		private UsbEndpointReader readEnpoint;

		public int lastDistance = 0;

		public int lastAngleX = 0;

		public int lastAngleY = 0;

		public TeslaM70RangeMeter()
		{

		}

		public bool openDevice()
		{
			UsbRegDeviceList allDevices = UsbDevice.AllDevices;
			UsbRegDeviceList usbRegDeviceList = new UsbRegDeviceList();
			usbRegDeviceList = usbRegDeviceList.FindAll(usbFinder);
			_teslaM70 = UsbDevice.OpenUsbDevice(usbFinder);
			if (_teslaM70 != null)
			{
				_teslaM70.Open();
				IUsbDevice usbDevice = _teslaM70 as IUsbDevice;
				if (usbDevice != null)
				{
					usbDevice.SetConfiguration(1);
					usbDevice.ClaimInterface(0);
				}

				writeEndpoint = _teslaM70.OpenEndpointWriter(WriteEndpointID.Ep01);
				readEnpoint = _teslaM70.OpenEndpointReader(ReadEndpointID.Ep02);
				return true;
			}

			return false;
		}

		private bool sendString(string cmd)
		{
			byte[] bytes = Encoding.Default.GetBytes(cmd);
			byte[] array = new byte[1024];
			ErrorCode errorCode = writeEndpoint.SubmitAsyncTransfer(bytes, 0, bytes.Length, 1000, out UsbTransfer _);
			errorCode = readEnpoint.Read(array, 3000, out int _);
			if (errorCode == ErrorCode.None && array[2] == 68)
			{
				lastDistance = ((array[3] << 24) | (array[4] << 16) | (array[5] << 8) | array[6]);
				lastAngleX = ((array[7] << 24) | (array[8] << 16) | (array[9] << 8) | array[10]);
				lastAngleY = ((array[11] << 24) | (array[12] << 16) | (array[13] << 8) | array[14]);
			}

			return errorCode == ErrorCode.None;
		}

		public void clearOffButton()
		{
			sendString("ATK009#");
		}

		public void readButton()
		{
			sendString("ATK001#");
		}

		public void readCurrentRecord()
		{
			sendString("ATD001#");
		}

		public void readCurrentScreenRecord()
		{
			sendString("ATI001#");
		}

		public void turnOnLaser()
		{
			byte[] bytes = Encoding.Default.GetBytes("ATI001#");
			byte[] array = new byte[1024];
			ErrorCode errorCode = writeEndpoint.SubmitAsyncTransfer(bytes, 0, bytes.Length, 1000, out _);
			byte[] buffer = new byte[7]
			{
				65,
				84,
				83,
				48,
				48,
				49,
				35
			};
			if (writeEndpoint.Write(buffer, 3000, out int _) == ErrorCode.None)
			{
				UsbSetupPacket setupPacket = new UsbSetupPacket(64, 1, 1, 0, 0);
				byte[] buffer2 = new byte[100];
				var ok = _teslaM70.ControlTransfer(ref setupPacket, buffer2, 100, out int _);
			}
		}

		public int GetReading()
		{
			throw new System.NotImplementedException();
		}

		public void ToggleLaser(bool enable)
		{
			throw new System.NotImplementedException();
		}
	}
}