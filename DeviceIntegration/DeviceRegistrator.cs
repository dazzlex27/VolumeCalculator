using System;
using DeviceIntegration.Exceptions;
using Primitives;

namespace DeviceIntegration
{
	public class DeviceRegistrator : IDeviceRegistrator
	{
		private readonly DeviceDefinitions _definitions;

		public DeviceRegistrator(DeviceDefinitions definitions)
		{
			_definitions = definitions;
		}

		public void RegisterDevice(DeviceType deviceType, string name, Type typeInstance)
		{
			switch (deviceType)
			{
				case DeviceType.DepthCamera:
					RegisterFrameProvider(name, typeInstance);
					break;
				case DeviceType.Scales:
					RegisterScales(name, typeInstance);
					break;
				case DeviceType.IoCircuit:
					RegisterIoCircuit(name, typeInstance);
					break;
				case DeviceType.BarcodeScanner:
					RegisterBarcodeScanner(name, typeInstance);
					break;
				case DeviceType.RangeMeter:
					RegisterRangeMeter(name, typeInstance);
					break;
				case DeviceType.IpCamera:
					RegisterIpCamera(name, typeInstance);
					break;
				default:
					throw new UnsupportedDeviceTypeException("Failed to register device: ", name, deviceType);
			}
		}

		private void RegisterFrameProvider(string name, Type frameProvider)
		{
			var frameProviderDefs = _definitions.FrameProviders;
			if (frameProviderDefs.ContainsKey(name))
				throw new DeviceAlreadyExistsException("Frame provider", name);

			frameProviderDefs.Add(name, frameProvider);
		}

		private void RegisterScales(string name, Type scales)
		{
			var scalesDefs = _definitions.Scales;
			if (scalesDefs.ContainsKey(name))
				throw new DeviceAlreadyExistsException("Scales", name);

			scalesDefs.Add(name, scales);
		}

		private void RegisterIoCircuit(string name, Type circuit)
		{
			var circuitDefs = _definitions.IoCircuits;
			if (circuitDefs.ContainsKey(name))
				throw new DeviceAlreadyExistsException("IoCircuit", name);

			circuitDefs.Add(name, circuit);
		}

		private void RegisterBarcodeScanner(string name, Type barcodeScanner)
		{
			var barcodeScannerDefs = _definitions.BarcodeScanners;
			if (barcodeScannerDefs.ContainsKey(name))
				throw new DeviceAlreadyExistsException("BarcodeScanner", name);

			barcodeScannerDefs.Add(name, barcodeScanner);
		}

		private void RegisterRangeMeter(string name, Type rangeMeter)
		{
			var rangeMeterDefs = _definitions.RangeMeters;
			if (rangeMeterDefs.ContainsKey(name))
				throw new DeviceAlreadyExistsException("RangeMeter", name);

			rangeMeterDefs.Add(name, rangeMeter);
		}

		private void RegisterIpCamera(string name, Type ipCamera)
		{
			var ipCameraDefs = _definitions.IpCameras;
			if (ipCameraDefs.ContainsKey(name))
				throw new DeviceAlreadyExistsException("IpCamera", name);

			ipCameraDefs.Add(name, ipCamera);
		}
	}
}
