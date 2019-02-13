using System;

namespace DeviceIntegration.IoCircuits
{
	public interface IIoCircuit : IDisposable
	{
		void ToggleRelay(int relayNum, bool state);
	}
}