using Primitives;
using System;

namespace DeviceIntegration.Exceptions
{
	[Serializable]
	internal class UnsupportedDeviceTypeException : Exception
	{
		public string DeviceName { get; }

		public DeviceType Type { get; }

		public UnsupportedDeviceTypeException() { }

		public UnsupportedDeviceTypeException(string message)
			: base(message) { }

		public UnsupportedDeviceTypeException(string message, Exception inner)
			: base(message, inner) { }

		public UnsupportedDeviceTypeException(string message, string deviceName, DeviceType type)
			: base(message)
		{
			DeviceName = deviceName;
			Type = type;
		}
	}
}
