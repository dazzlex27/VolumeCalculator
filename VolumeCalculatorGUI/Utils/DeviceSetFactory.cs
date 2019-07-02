using System;
using System.Collections.Generic;
using System.IO;
using System.Management;
using System.Text;
using DeviceIntegration;
using DeviceIntegration.IoCircuits;
using DeviceIntegration.RangeMeters;
using DeviceIntegration.Scales;
using DeviceIntegration.Scanners;
using FrameProviders;
using Primitives.Logging;
using Primitives.Settings;
using ProcessingUtils;

namespace VolumeCalculatorGUI.Utils
{
	internal class DeviceSetFactory
	{
		public DeviceSet CreateDeviceSet(ILogger logger, IoSettings settings)
		{
			IScales scales = null;
			var barcodeScanners = new List<IBarcodeScanner>();
			IIoCircuit ioCircuit = null;
			IRangeMeter rangeMeter = null;

			var frameProviderName = settings.ActiveCameraName;
			logger.LogInfo($"Creating frame provider \"{frameProviderName}\"...");
			var frameProvider = FrameProvidersCommon.CreateRequestedFrameProvider(frameProviderName, logger);
			frameProvider.ColorCameraFps = 5;
			frameProvider.DepthCameraFps = 5;
			frameProvider.Start();

			foreach (var entry in settings.ActiveScanners)
			{
				logger.LogInfo($"Creating scanner \"{entry.Name}\"...");

				if (entry.Port == string.Empty && entry.Name != "keyboard")
					continue;

				var scanner = DeviceIntegrationCommon.CreateRequestedScanner(entry.Name, logger, entry.Port);
				barcodeScanners.Add(scanner);
			}

			var scalesName = settings.ActiveScalesName;
			var scalesPort = settings.ScalesPort;
			if (scalesPort != string.Empty)
			{
				logger.LogInfo($"Creating scales \"{scalesName}\"...");
				scales = DeviceIntegrationCommon.CreateRequestedScales(scalesName, logger, scalesPort);
			}

			var ioCircuitName = settings.ActiveIoCircuitName;
			var ioCircuitPort = settings.IoCircuitPort;
			if (ioCircuitPort != string.Empty)
			{
				logger.LogInfo($"Creating IO circuit \"{ioCircuitName}\"...");
				ioCircuit = DeviceIntegrationCommon.CreateRequestedIoCircuit(ioCircuitName, logger, ioCircuitPort);
			}

			var rangeMeterName = settings.ActiveRangeMeterName;
			var rangeMeterPort = settings.RangeMeterPort;
			if (rangeMeterPort != string.Empty)
			{
				logger.LogInfo($"Creating range meter \"{rangeMeterName}\"");
				rangeMeter = DeviceIntegrationCommon.CreateRequestedRangeMeter(rangeMeterName, logger, rangeMeterPort);
			}

			return new DeviceSet(frameProvider, scales, barcodeScanners, ioCircuit, rangeMeter);
		}

		public static byte[] GetMaskBytes()
		{
			var serial = "";
			try
			{
				var requestBytesString = Encoding.ASCII.GetString(IoUtils.GetHwBytes());
				var searcher = new ManagementObjectSearcher(requestBytesString);

				foreach (var o in searcher.Get())
				{
					var wmiHd = (ManagementObject)o;

					serial = wmiHd["SerialNumber"].ToString();
				}
			}
			catch (Exception ex)
			{
				Directory.CreateDirectory("c:/temp");
				using (var f = File.AppendText("c:/temp"))
				{
					f.WriteLine($"s1 f{ex}");
				}

				return new byte[0];
			}

			return string.IsNullOrEmpty(serial) ? new byte[] {0} : Encoding.ASCII.GetBytes(serial);
		}
	}
}