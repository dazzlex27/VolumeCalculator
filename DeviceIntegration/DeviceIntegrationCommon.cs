using System;
using System.Collections.Generic;
using System.Net.Http;
using DeviceIntegration.Cameras;
using DeviceIntegration.FrameProviders;
using DeviceIntegration.IoCircuits;
using DeviceIntegration.RangeMeters;
using DeviceIntegration.Scales;
using Primitives.Logging;
using Primitives.Settings;

namespace DeviceIntegration
{
	public static class DeviceIntegrationCommon
	{
		private static readonly Dictionary<string, Type> FrameProviders;
		private static readonly Dictionary<string, Type> Scales;
		private static readonly Dictionary<string, Type> IoCircuits;
		private static readonly Dictionary<string, Type> BarcodeScanners;
		private static readonly Dictionary<string, Type> RangeMeters;
		private static readonly Dictionary<string, Type> IpCameras;

		static DeviceIntegrationCommon()
		{
			FrameProviders = new Dictionary<string, Type>();
			Scales = new Dictionary<string, Type>();
			IoCircuits = new Dictionary<string, Type>();
			BarcodeScanners = new Dictionary<string, Type>();
			RangeMeters = new Dictionary<string, Type>();
			IpCameras = new Dictionary<string, Type>();
		}

		public static void RegisterFrameProvider(string name, Type type)
		{
			if (FrameProviders.ContainsKey(name))
				throw new DeviceAlreadyExistsException("Frame provider", name);

			FrameProviders.Add(name, type);
		}

		public static void RegisterScales(string name, Type scales)
		{
			if (Scales.ContainsKey(name))
				throw new DeviceAlreadyExistsException("Scales", name);

			Scales.Add(name, scales);
		}

		public static void RegisterIoCircuit(string name, Type circuit)
		{
			if (IoCircuits.ContainsKey(name))
				throw new DeviceAlreadyExistsException("IoCircuit", name);

			IoCircuits.Add(name, circuit);
		}

		public static void RegisterBarcodeScanner(string name, Type scanner)
		{
			if (BarcodeScanners.ContainsKey(name))
				throw new DeviceAlreadyExistsException("BarcodeScanner", name);

			BarcodeScanners.Add(name, scanner);
		}

		public static void RegisterRangeMeter(string name, Type rangeMeter)
		{
			if (RangeMeters.ContainsKey(name))
				throw new DeviceAlreadyExistsException("RangeMeter", name);

			RangeMeters.Add(name, rangeMeter);
		}

		public static void RegisterIpCamera(string name, Type ipCamera)
		{
			if (IpCameras.ContainsKey(name))
				throw new DeviceAlreadyExistsException("IpCamera", name);

			IpCameras.Add(name, ipCamera);
		}

		public static IFrameProvider CreateRequestedFrameProvider(string name, ILogger logger)
		{
			var targetType = FrameProviders[name];
			if (targetType == null)
				throw new DeviceNotFoundException("FrameProvider", name);

			return (IFrameProvider)Activator.CreateInstance(targetType.GetType(), logger);
		}

		public static IScales CreateRequestedScales(string name, ILogger logger, string port, int minWeight)
		{
			var targetType = Scales[name];
			if (targetType == null)
				throw new DeviceNotFoundException("Scales", name);

			return (IScales)Activator.CreateInstance(targetType.GetType(), logger, port, minWeight);
		}

		public static IBarcodeScanner CreateRequestedScanner(string name, ILogger logger, string port)
		{
			var targetType = BarcodeScanners[name];
			if (targetType == null)
				throw new DeviceNotFoundException("BarcodeScanner", name);

			return (IBarcodeScanner)Activator.CreateInstance(targetType.GetType(), logger, port);
		}

		public static IIoCircuit CreateRequestedIoCircuit(string name, ILogger logger, string port)
		{
			var targetType = IoCircuits[name];
			if (targetType == null)
				throw new DeviceNotFoundException("IoCircuit", name);

			return (IIoCircuit)Activator.CreateInstance(targetType.GetType(), logger, port);
		}

		public static IRangeMeter CreateRequestedRangeMeter(string name, ILogger logger)
		{
			var targetType = RangeMeters[name];
			if (targetType == null)
				throw new DeviceNotFoundException("RangeMeter", name);

			return (IRangeMeter)Activator.CreateInstance(targetType.GetType(), logger);
		}

		public static IIpCamera CreateRequestedIpCamera(IpCameraSettings settings, HttpClient httpClient, ILogger logger)
		{
			if (settings == null)
				return null;

			var name = settings.CameraName;
			var targetType = IpCameras[name];
			if (targetType == null)
				throw new DeviceNotFoundException("IpCamera", name);

			return (IIpCamera)Activator.CreateInstance(targetType.GetType(), logger, httpClient, settings);
		}
	}
}
