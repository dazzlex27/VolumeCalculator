using System.Runtime.InteropServices;

namespace FrameProcessor.Native
{
	[StructLayout(LayoutKind.Sequential)]
	internal unsafe struct ColorImage
	{
		public int Width;
		public int Height;
		public byte* Data;
		public byte BytesPerPixel;
	}
}