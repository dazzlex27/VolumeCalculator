using System;
using System.Collections.Generic;

namespace DeviceIntegration
{
	public class DeviceDefinitions
	{
		public readonly Dictionary<string, Type> FrameProviders;
		public readonly Dictionary<string, Type> Scales;
		public readonly Dictionary<string, Type> IoCircuits;
		public readonly Dictionary<string, Type> BarcodeScanners;
		public readonly Dictionary<string, Type> RangeMeters;
		public readonly Dictionary<string, Type> IpCameras;

		public DeviceDefinitions()
		{
			FrameProviders = new Dictionary<string, Type>();
			Scales = new Dictionary<string, Type>();
			IoCircuits = new Dictionary<string, Type>();
			BarcodeScanners = new Dictionary<string, Type>();
			RangeMeters = new Dictionary<string, Type>();
			IpCameras = new Dictionary<string, Type>();
		}
	}
}
