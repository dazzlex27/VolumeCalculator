﻿using System;
using DeviceIntegration.IoCircuits;
using DeviceIntegration.Scales;
using DeviceIntegrations.IoCircuits;
using DeviceIntegrations.Scales;
using DeviceIntegrations.Scanners;
using Primitives.Logging;

namespace DeviceIntegration
{
	public static class DeviceIntegrationCommon
	{
		public static IScales CreateRequestedScales(string name, ILogger logger, string port)
		{
			switch (name)
			{
				case "fakescales":
					return new FakeScales(logger);
				case "massak":
					return new MassaKScales(logger, port);
				case "casm":
					return new CasMScales(logger, port);
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
				case "ke24usbr":
					return new KeUsb24RCircuit(logger, port);
				default:
					logger.LogError($"Failed to create IO circuit by the name \"{name}\"");
					throw new NotSupportedException();
			}
		}
	}
}