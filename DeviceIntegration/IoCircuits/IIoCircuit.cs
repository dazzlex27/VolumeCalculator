using System;

namespace DeviceIntegration.IoCircuits
{
	public interface IIoCircuit : IDisposable
	{
		void WriteData(string data);

		void ToggleRelay(int relayNum, bool state);

		int PollLine(int lineNum);
	}
}
