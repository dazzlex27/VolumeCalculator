using System;

namespace DeviceIntegrations.IoCircuits
{
	public interface IIoCircuit : IDisposable
	{
		void ToggleRelay(int relayNum, bool state);
	}
}