using System.Runtime.InteropServices;

namespace VolumeCalculatorGUI.Entities
{
	[StructLayout(LayoutKind.Sequential)]
	public struct ObjDimDescription
	{
		public int Length;
		public int Width;
		public int Height;
	}
}