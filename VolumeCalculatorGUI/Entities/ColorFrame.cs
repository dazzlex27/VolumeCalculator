using System.Runtime.InteropServices;

namespace DepthMapProcessorGUI.Entities
{
	[StructLayout(LayoutKind.Sequential)]
	public unsafe struct ColorFrame
	{
		public int Width;
		public int Height;
		public byte* Data;
	}
}