using System.Runtime.InteropServices;

namespace FrameProcessor.Native
{
	[StructLayout(LayoutKind.Sequential)]
	public struct RelPoint
	{
		public float X;
		public float Y;
	}
}