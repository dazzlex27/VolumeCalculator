using System;
using System.Collections.Generic;

namespace DeviceIntegration
{
	internal class DeviceDefinitions
	{
		public static readonly Dictionary<string, Type> FrameProviders;
		public static readonly Dictionary<string, Type> Scales;
		public static readonly Dictionary<string, Type> IoCircuits;
		public static readonly Dictionary<string, Type> BarcodeScanners;
		public static readonly Dictionary<string, Type> RangeMeters;
		public static readonly Dictionary<string, Type> IpCameras;

		static DeviceDefinitions()
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
