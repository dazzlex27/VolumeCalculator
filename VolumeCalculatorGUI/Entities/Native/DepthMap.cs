using System.Runtime.InteropServices;

namespace VolumeCalculatorGUI.Entities.Native
{
	[StructLayout(LayoutKind.Sequential)]
	internal unsafe struct DepthMap
	{
		public int Width;
		public int Height;
		public short* Data;
	}
}