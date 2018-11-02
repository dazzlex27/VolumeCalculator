using System.Runtime.InteropServices;

namespace VolumeCalculatorGUI.Entities.Native
{
	[StructLayout(LayoutKind.Sequential)]
	internal struct RelRect
	{
		public float X;
		public float Y;
		public float Width;
		public float Height;
	};
}