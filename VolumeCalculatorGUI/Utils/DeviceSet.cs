using System.Collections.Generic;
using DeviceIntegration.IoCircuits;
using DeviceIntegration.RangeMeters;
using DeviceIntegration.Scales;
using DeviceIntegration.Scanners;
using FrameProviders;

namespace VolumeCalculatorGUI.Utils
{
	internal class DeviceSet
	{
		public FrameProvider FrameProvider { get; }

		public IScales Scales { get; }

		public IReadOnlyList<IBarcodeScanner> Scanners { get; }

		public IIoCircuit IoCircuit { get; }

		public IRangeMeter RangeMeter { get; }

		public DeviceSet(FrameProvider frameProvider, IScales scales, IEnumerable<IBarcodeScanner> scanners,
			IIoCircuit ioCircuit, IRangeMeter rangeMeter)
		{
			FrameProvider = frameProvider;
			Scales = scales;
			Scanners = new List<IBarcodeScanner>(scanners);
			IoCircuit = ioCircuit;
			RangeMeter = rangeMeter;
		}
	}
}