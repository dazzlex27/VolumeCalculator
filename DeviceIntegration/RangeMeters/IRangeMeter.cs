using System;

namespace DeviceIntegration.RangeMeters
{
	public interface IRangeMeter : IDisposable
	{
		void ToggleLaser(bool enable);

		long GetReading();
	}
}