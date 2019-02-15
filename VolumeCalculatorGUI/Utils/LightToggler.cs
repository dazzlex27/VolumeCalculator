using DeviceIntegration.IoCircuits;

namespace VolumeCalculatorGUI.Utils
{
	internal class LightToggler
	{
		private readonly IIoCircuit _circuit;

		public LightToggler(IIoCircuit circuit)
		{
			_circuit = circuit;
		}

		public void ToggleReady()
		{
			_circuit.ToggleRelay(2, false);
			_circuit.ToggleRelay(3, true);
			_circuit.ToggleRelay(4, true);
		}

		public void ToggleError()
		{
			_circuit.ToggleRelay(3, true);
			_circuit.ToggleRelay(3, true);
			_circuit.ToggleRelay(4, false);
		}

		public void ToggleMeasuring()
		{
			_circuit.ToggleRelay(4, true);
			_circuit.ToggleRelay(3, false);
			_circuit.ToggleRelay(4, true);
		}
	}
}