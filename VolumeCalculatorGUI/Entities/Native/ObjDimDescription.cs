using System.Runtime.InteropServices;

namespace VolumeCalculatorGUI.Entities.Native
{
	[StructLayout(LayoutKind.Sequential)]
	public struct ObjDimDescription
	{
		public int Length;
		public int Width;
		public int Height;
	}
}