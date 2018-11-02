using System.Runtime.InteropServices;

namespace VolumeCalculatorGUI.Entities.Native
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