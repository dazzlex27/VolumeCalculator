using System.Runtime.InteropServices;

namespace Primitives
{
	[StructLayout(LayoutKind.Sequential)]
	public unsafe struct ColorFrame
	{
		public int Width;
		public int Height;
		public byte* Data;
	}
}