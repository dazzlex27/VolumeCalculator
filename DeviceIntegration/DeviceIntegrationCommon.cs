using System;
using System.Net.Http;
using DeviceIntegration.Cameras;
using DeviceIntegration.IoCircuits;
using DeviceIntegration.RangeMeters;
using DeviceIntegration.Scales;
using DeviceIntegration.Scanners;
using Primitives.Logging;
using Primitives.Settings;

namespace DeviceIntegration
{
	public static class DeviceIntegrationCommon
	{
		public static IScales CreateRequestedScales(string name, ILogger logger, string port, int minWeight)
		{
			switch (name)
			{
				case "fakescales":
					return new FakeScales(logger);
				case "massak":
					return new MassaKScales(logger, port, minWeight);
				case "casm":
					return new CasMScales(logger, port, minWeight);
				case "ci2001a":
					return new Ci2001AScales(logger, port, minWeight);
				case "oka":
					return new OkaScales(logger, port, minWeight);
				default:
					logger.LogError($"Failed to create scales by the name \"{name}\"");
					throw new NotSupportedException();
			}
		}

		public static IBarcodeScanner CreateRequestedScanner(string name, ILogger logger, string port)
		{
			switch (name)
			{
				case "generic":
					return new GenericSerialPortBarcodeScanner(logger, port);
				case "keyboard":
					return new GenericKeyboardBarcodeScanner(logger);
				default:
					logger.LogError($"Failed to create scanner by the name \"{name}\"");
					throw new NotSupportedException();
			}
		}

		public static IIoCircuit CreateRequestedIoCircuit(string name, ILogger logger, string port)
		{
			switch (name)
			{
				case "keusb24r":
					return new KeUsb24RCircuit(logger, port);
				default:
					logger.LogError($"Failed to create IO circuit by the name \"{name}\"");
					throw new NotSupportedException();
			}
		}

		public static IRangeMeter CreateRequestedRangeMeter(string name, ILogger logger, string port)
		{
			switch (name)
			{
				case "custom":
					return new TeslaM70RangeMeter(logger);
				case "fake":
					return new FakeRangeMeter(logger);
				default:
					logger.LogError($"Failed to create range meter by the name \"{name}\"");
					throw new NotSupportedException();
			}
		}

		public static IIpCamera CreateRequestedIpCamera(IpCameraSettings settings, HttpClient httpClient, ILogger logger)
		{
			if (settings == null)
				return null;

			var name = settings.CameraName;
			switch (name)
			{
				case "proline2520":
					return new Proline2520Camera(logger, httpClient, settings);
				default:
					logger.LogError($"Failed to create ip camera by the name \"{name}\"");
					throw new NotSupportedException();
			}
		}
	}
}