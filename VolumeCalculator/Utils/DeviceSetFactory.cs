using System;
using System.Collections.Generic;
using System.IO;
using System.Management;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using DeviceIntegration;
using DeviceIntegration.Cameras;
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
		public DeviceSet CreateDeviceSet(ILogger logger, HttpClient httpClient, IoSettings settings)
		{
			IScales scales = null;
			var barcodeScanners = new List<IBarcodeScanner>();
			IIoCircuit ioCircuit = null;
			IRangeMeter rangeMeter = null;
			IIpCamera ipCamera = null;

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
			var scalesDataIsCorrect = !string.IsNullOrEmpty(scalesName) && !string.IsNullOrEmpty(scalesPort);
			if (scalesDataIsCorrect)
			{
                var minWeight = settings.ScalesMinWeight;

				logger.LogInfo($"Creating scales \"{scalesName}\", minWeight={minWeight}...");
				scales = DeviceIntegrationCommon.CreateRequestedScales(scalesName, logger, scalesPort, minWeight);
			}

			var ioCircuitName = settings.ActiveIoCircuitName;
			var ioCircuitPort = settings.IoCircuitPort;
			var ioCircuitDataIsCorrect = !string.IsNullOrEmpty(ioCircuitName) && !string.IsNullOrEmpty(ioCircuitPort);
			if (ioCircuitDataIsCorrect)
			{
				logger.LogInfo($"Creating IO circuit \"{ioCircuitName}\"...");
				ioCircuit = DeviceIntegrationCommon.CreateRequestedIoCircuit(ioCircuitName, logger, ioCircuitPort);
			}

			var rangeMeterName = settings.ActiveRangeMeterName;
			var rangeMeterDataIsCorrect = !string.IsNullOrEmpty(rangeMeterName);
			if (rangeMeterDataIsCorrect)
			{
				logger.LogInfo($"Creating range meter \"{rangeMeterName}\"");
				rangeMeter = DeviceIntegrationCommon.CreateRequestedRangeMeter(rangeMeterName, logger);
			}

			var cameraSettings = settings.IpCameraSettings;
			if (!string.IsNullOrEmpty(cameraSettings.CameraName))
			{
				logger.LogInfo($"Creating IP camera \"{cameraSettings.CameraName}\"");
				ipCamera = DeviceIntegrationCommon.CreateRequestedIpCamera(cameraSettings, httpClient, logger);

				var cameraFailed = false;
				Exception cameraEx = null;

				Task.Run(async () =>
				{
					try
					{
						var connected = await ipCamera.ConnectAsync();
						await ipCamera.GoToPresetAsync(settings.IpCameraSettings.ActivePreset);
					}
					catch (Exception ex)
					{
						cameraFailed = true;
						cameraEx = ex;
					}
				});

				if (cameraFailed)
					throw cameraEx;
			}

			return new DeviceSet(frameProvider, scales, barcodeScanners, ioCircuit, rangeMeter, ipCamera);
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