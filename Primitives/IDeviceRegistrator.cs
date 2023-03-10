using System;

namespace Primitives
{
	public interface IDeviceRegistrator
	{
		void RegisterDevice(DeviceType deviceType, string name, Type typeReference);
	}
}
