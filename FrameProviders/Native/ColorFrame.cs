using System.Runtime.InteropServices;

namespace FrameProviders.Native
{
	[StructLayout(LayoutKind.Sequential)]
	public unsafe struct ColorFrame
	{
		public int Width;
		public int Height;
		public byte* Data;
	}
}