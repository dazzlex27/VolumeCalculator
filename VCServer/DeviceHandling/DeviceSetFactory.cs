using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using DeviceIntegration;
using DeviceIntegration.IoCircuits;
using DeviceIntegration.RangeMeters;
using DeviceIntegration.Scales;
using Primitives.Logging;
using Primitives.Settings;

namespace VCServer.DeviceHandling
{
	internal static class DeviceSetFactory
	{
		public static DeviceSet CreateDeviceSet(ILogger logger, HttpClient httpClient, IoSettings settings,
			DeviceFactory factory)
		{
			IScales scales = null;
			var barcodeScanners = new List<IBarcodeScanner>();
			IIoCircuit ioCircuit = null;
			IRangeMeter rangeMeter = null;

			var frameProviderName = settings.ActiveCameraName;
			logger.LogInfo($"Creating frame provider \"{frameProviderName}\"...");
			var frameProvider = factory.CreateRequestedFrameProvider(frameProviderName, logger);
			frameProvider.ColorCameraFps = 5;
			frameProvider.DepthCameraFps = 5;
			frameProvider.Start();

			foreach (var entry in settings.ActiveScanners)
			{
				logger.LogInfo($"Creating scanner \"{entry.Name}\"...");

				if (entry.Port == string.Empty && entry.Name != "keyboard")
					continue;

				var scanner = factory.CreateRequestedScanner(entry.Name, logger, entry.Port);
				barcodeScanners.Add(scanner);
			}

			var scalesName = settings.ActiveScales.Name;
			var scalesPort = settings.ActiveScales.Port;
			var scalesDataIsCorrect = !string.IsNullOrEmpty(scalesName) && !string.IsNullOrEmpty(scalesPort);
			if (scalesDataIsCorrect)
			{
				var minWeight = settings.ActiveScales.MinWeight;

				logger.LogInfo($"Creating scales \"{scalesName}\", minWeight={minWeight}...");
				scales = factory.CreateRequestedScales(scalesName, logger, scalesPort, minWeight);
			}

			var ioCircuitName = settings.ActiveIoCircuit.Name;
			var ioCircuitPort = settings.ActiveIoCircuit.Port;
			var ioCircuitDataIsCorrect = !string.IsNullOrEmpty(ioCircuitName) && !string.IsNullOrEmpty(ioCircuitPort);
			if (ioCircuitDataIsCorrect)
			{
				logger.LogInfo($"Creating IO circuit \"{ioCircuitName}\"...");
				ioCircuit = factory.CreateRequestedIoCircuit(ioCircuitName, logger, ioCircuitPort);
			}

			var rangeMeterName = settings.ActiveRangeMeterName;
			var rangeMeterDataIsCorrect = !string.IsNullOrEmpty(rangeMeterName);
			if (rangeMeterDataIsCorrect)
			{
				logger.LogInfo($"Creating range meter \"{rangeMeterName}\"");
				rangeMeter = factory.CreateRequestedRangeMeter(rangeMeterName, logger);
			}

			var cameraSettings = settings.IpCameraSettings;
			if (string.IsNullOrEmpty(cameraSettings.CameraName))
				return new DeviceSet(frameProvider, scales, barcodeScanners, ioCircuit, rangeMeter, null);

			logger.LogInfo($"Creating IP camera \"{cameraSettings.CameraName}\"");
			var ipCamera = factory.CreateRequestedIpCamera(cameraSettings, httpClient, logger);

			var cameraFailed = false;
			Exception cameraEx = null;

			Task.FromResult(async () =>
			{
				try
				{
					await ipCamera.ConnectAsync();
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

			return new DeviceSet(frameProvider, scales, barcodeScanners, ioCircuit, rangeMeter, ipCamera);
		}
	}
}
