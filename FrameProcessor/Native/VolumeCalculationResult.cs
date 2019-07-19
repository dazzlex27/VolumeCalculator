using System.Runtime.InteropServices;

namespace FrameProcessor.Native
{
	[StructLayout(LayoutKind.Sequential)]
	internal struct VolumeCalculationResult
	{
		public int LengthMm;
		public int WidthMm;
		public int HeightMm;
		public double VolumeCmCb;
	}
}