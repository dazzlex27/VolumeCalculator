using System;
using DeviceIntegration.Exceptions;

namespace DeviceIntegration
{
	public static class DeviceRegistrator
	{
		public static void RegisterFrameProvider(string name, Type frameProvider)
		{
			var frameProviderDefs = DeviceDefinitions.FrameProviders;
			if (frameProviderDefs.ContainsKey(name))
				throw new DeviceAlreadyExistsException("Frame provider", name);

			frameProviderDefs.Add(name, frameProvider);
		}

		public static void RegisterScales(string name, Type scales)
		{
			var scalesDefs = DeviceDefinitions.Scales;
			if (scalesDefs.ContainsKey(name))
				throw new DeviceAlreadyExistsException("Scales", name);

			scalesDefs.Add(name, scales);
		}

		public static void RegisterIoCircuit(string name, Type circuit)
		{
			var circuitDefs = DeviceDefinitions.IoCircuits;
			if (circuitDefs.ContainsKey(name))
				throw new DeviceAlreadyExistsException("IoCircuit", name);

			circuitDefs.Add(name, circuit);
		}

		public static void RegisterBarcodeScanner(string name, Type barcodeScanner)
		{
			var barcodeScannerDefs = DeviceDefinitions.BarcodeScanners;
			if (barcodeScannerDefs.ContainsKey(name))
				throw new DeviceAlreadyExistsException("BarcodeScanner", name);

			barcodeScannerDefs.Add(name, barcodeScanner);
		}

		public static void RegisterRangeMeter(string name, Type rangeMeter)
		{
			var rangeMeterDefs = DeviceDefinitions.RangeMeters;
			if (rangeMeterDefs.ContainsKey(name))
				throw new DeviceAlreadyExistsException("RangeMeter", name);

			rangeMeterDefs.Add(name, rangeMeter);
		}

		public static void RegisterIpCamera(string name, Type ipCamera)
		{
			var ipCameraDefs = DeviceDefinitions.IpCameras;
			if (ipCameraDefs.ContainsKey(name))
				throw new DeviceAlreadyExistsException("IpCamera", name);

			ipCameraDefs.Add(name, ipCamera);
		}

	}
}
