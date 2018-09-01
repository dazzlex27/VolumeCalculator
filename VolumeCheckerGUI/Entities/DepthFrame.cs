using System.Runtime.InteropServices;

namespace VolumeCheckerGUI.Entities
{
	[StructLayout(LayoutKind.Sequential)]
	public unsafe struct DepthFrame
	{
		public int Width;
		public int Height;
		public short* Data;
	}
}