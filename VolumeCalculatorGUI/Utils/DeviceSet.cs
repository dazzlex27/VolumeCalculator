using System.Collections.Generic;
using DeviceIntegration.Scales;
using DeviceIntegrations.IoCircuits;
using DeviceIntegrations.Scanners;
using FrameProviders;

namespace VolumeCalculatorGUI.Utils
{
	internal class DeviceSet
	{
		public FrameProvider FrameProvider { get; }

		public IScales Scales { get; }

		public IReadOnlyList<IBarcodeScanner> Scanners { get; }

		public IIoCircuit IoCircuit { get; }

		public DeviceSet(FrameProvider frameProvider, IScales scales, IReadOnlyList<IBarcodeScanner> scanners,
			IIoCircuit ioCircuit)
		{
			FrameProvider = frameProvider;
			Scales = scales;
			Scanners = new List<IBarcodeScanner>(scanners);
			IoCircuit = ioCircuit;
		}
	}
}