using System.Runtime.InteropServices;

namespace FrameProcessor.Native
{
	[StructLayout(LayoutKind.Sequential)]
	internal unsafe struct DepthMap
	{
		public int Width;
		public int Height;
		public short* Data;
	}
}