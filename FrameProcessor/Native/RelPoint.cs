using System.Runtime.InteropServices;

namespace FrameProcessor.Native
{
	[StructLayout(LayoutKind.Sequential)]
	internal struct RelPoint
	{
		public float X;
		public float Y;
	}
}
