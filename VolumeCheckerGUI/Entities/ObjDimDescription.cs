using System.Runtime.InteropServices;

namespace VolumeCheckerGUI.Entities
{
	[StructLayout(LayoutKind.Sequential)]
	public struct ObjDimDescription
	{
		public short Width;
		public short Height;
		public short Depth;
	}
}