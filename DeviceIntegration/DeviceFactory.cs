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
	public class DeviceFactory
	{
		private readonly DeviceDefinitions _definitions;

		public DeviceFactory(DeviceDefinitions definitions)
		{
			_definitions = definitions;
		}

		public IFrameProvider CreateRequestedFrameProvider(string name, ILogger logger)
		{
			var targetType = _definitions.FrameProviders[name];
			if (targetType == null)
				throw new DeviceNotFoundException("FrameProvider", name);

			return (IFrameProvider)Activator.CreateInstance(targetType, logger);
		}

		public IScales CreateRequestedScales(string name, ILogger logger, string port, int minWeight)
		{
			var targetType = _definitions.Scales[name];
			if (targetType == null)
				throw new DeviceNotFoundException("Scales", name);

			return (IScales)Activator.CreateInstance(targetType, logger, port, minWeight);
		}

		public IBarcodeScanner CreateRequestedScanner(string name, ILogger logger, string port)
		{
			var targetType = _definitions.BarcodeScanners[name];
			if (targetType == null)
				throw new DeviceNotFoundException("BarcodeScanner", name);

			return (IBarcodeScanner)Activator.CreateInstance(targetType, logger, port);
		}

		public IIoCircuit CreateRequestedIoCircuit(string name, ILogger logger, string port)
		{
			var targetType = _definitions.IoCircuits[name];
			if (targetType == null)
				throw new DeviceNotFoundException("IoCircuit", name);

			return (IIoCircuit)Activator.CreateInstance(targetType, logger, port);
		}

		public IRangeMeter CreateRequestedRangeMeter(string name, ILogger logger)
		{
			var targetType = _definitions.RangeMeters[name];
			if (targetType == null)
				throw new DeviceNotFoundException("RangeMeter", name);

			return (IRangeMeter)Activator.CreateInstance(targetType, logger);
		}

		public IIpCamera CreateRequestedIpCamera(IpCameraSettings settings, HttpClient httpClient, ILogger logger)
		{
			if (settings == null)
				return null;

			var name = settings.CameraName;
			var targetType = _definitions.IpCameras[name];
			if (targetType == null)
				throw new DeviceNotFoundException("IpCamera", name);

			return (IIpCamera)Activator.CreateInstance(targetType, logger, httpClient, settings);
		}
	}
}
