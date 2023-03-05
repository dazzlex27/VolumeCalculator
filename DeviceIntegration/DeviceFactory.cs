using DeviceIntegration.Cameras;
using DeviceIntegration.Exceptions;
using DeviceIntegration.FrameProviders;
using DeviceIntegration.IoCircuits;
using DeviceIntegration.RangeMeters;
using DeviceIntegration.Scales;
using Primitives.Logging;
using Primitives.Settings;
using System;
using System.Net.Http;

namespace DeviceIntegration
{
	public static class DeviceFactory
	{
		public static IFrameProvider CreateRequestedFrameProvider(string name, ILogger logger)
		{
			var targetType = DeviceDefinitions.FrameProviders[name];
			if (targetType == null)
				throw new DeviceNotFoundException("FrameProvider", name);

			return (IFrameProvider)Activator.CreateInstance(targetType, logger);
		}

		public static IScales CreateRequestedScales(string name, ILogger logger, string port, int minWeight)
		{
			var targetType = DeviceDefinitions.Scales[name];
			if (targetType == null)
				throw new DeviceNotFoundException("Scales", name);

			return (IScales)Activator.CreateInstance(targetType, logger, port, minWeight);
		}

		public static IBarcodeScanner CreateRequestedScanner(string name, ILogger logger, string port)
		{
			var targetType = DeviceDefinitions.BarcodeScanners[name];
			if (targetType == null)
				throw new DeviceNotFoundException("BarcodeScanner", name);

			return (IBarcodeScanner)Activator.CreateInstance(targetType, logger, port);
		}

		public static IIoCircuit CreateRequestedIoCircuit(string name, ILogger logger, string port)
		{
			var targetType = DeviceDefinitions.IoCircuits[name];
			if (targetType == null)
				throw new DeviceNotFoundException("IoCircuit", name);

			return (IIoCircuit)Activator.CreateInstance(targetType, logger, port);
		}

		public static IRangeMeter CreateRequestedRangeMeter(string name, ILogger logger)
		{
			var targetType = DeviceDefinitions.RangeMeters[name];
			if (targetType == null)
				throw new DeviceNotFoundException("RangeMeter", name);

			return (IRangeMeter)Activator.CreateInstance(targetType, logger);
		}

		public static IIpCamera CreateRequestedIpCamera(IpCameraSettings settings, HttpClient httpClient, ILogger logger)
		{
			if (settings == null)
				return null;

			var name = settings.CameraName;
			var targetType = DeviceDefinitions.IpCameras[name];
			if (targetType == null)
				throw new DeviceNotFoundException("IpCamera", name);

			return (IIpCamera)Activator.CreateInstance(targetType, logger, httpClient, settings);
		}
	}
}
