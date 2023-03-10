using System;

namespace DeviceIntegration.Exceptions
{
	[Serializable]
	internal class DeviceAlreadyExistsException : Exception
	{
		public string DeviceName { get; }

		public DeviceAlreadyExistsException() { }

		public DeviceAlreadyExistsException(string message)
			: base(message) { }

		public DeviceAlreadyExistsException(string message, Exception inner)
			: base(message, inner) { }

		public DeviceAlreadyExistsException(string message, string deviceName)
			: base(message)
		{
			DeviceName = deviceName;
		}
	}
}
