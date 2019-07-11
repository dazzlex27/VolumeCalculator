using System;
using System.Collections.Generic;
using DeviceIntegration.Cameras;
using DeviceIntegration.IoCircuits;
using DeviceIntegration.RangeMeters;
using DeviceIntegration.Scales;
using DeviceIntegration.Scanners;
using FrameProviders;

namespace VolumeCalculatorGUI.Utils
{
	internal class DeviceSet : IDisposable
	{
		public IFrameProvider FrameProvider { get; }

		public IScales Scales { get; }

		public IReadOnlyList<IBarcodeScanner> Scanners { get; }

		public IIoCircuit IoCircuit { get; }

		public IRangeMeter RangeMeter { get; }

		public IIpCamera IpCamera { get; }

		public DeviceSet(IFrameProvider frameProvider, IScales scales, IEnumerable<IBarcodeScanner> scanners,
			IIoCircuit ioCircuit, IRangeMeter rangeMeter, IIpCamera ipCamera)
		{
			FrameProvider = frameProvider;
			Scales = scales;
			Scanners = new List<IBarcodeScanner>(scanners);
			IoCircuit = ioCircuit;
			RangeMeter = rangeMeter;
			IpCamera = ipCamera;
		}

		public void Dispose()
		{
			FrameProvider?.Dispose();
			Scales?.Dispose();

			if (Scanners != null && Scanners.Count > 0)
			{
				foreach (var scanner in Scanners)
					scanner?.Dispose();
			}

			IoCircuit?.Dispose();
			RangeMeter?.Dispose();
		}

		public void TogglePause(bool pause)
		{
			Scales?.TogglePause(pause);

			if (Scanners != null && Scanners.Count > 0)
			{
				foreach (var scanner in Scanners)
					scanner?.TogglePause(pause);
			}
		}
	}
}