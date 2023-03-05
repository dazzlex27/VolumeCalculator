using System;

namespace DeviceIntegration.Exceptions
{
    [Serializable]
    internal class DeviceNotFoundException : Exception
    {
        public string DeviceName { get; set; }

        public DeviceNotFoundException() { }

        public DeviceNotFoundException(string message)
            : base(message) { }

        public DeviceNotFoundException(string message, Exception inner)
            : base(message, inner) { }

        public DeviceNotFoundException(string message, string deviceName)
            : base(message)
        {
            DeviceName = deviceName;
        }
    }
}
