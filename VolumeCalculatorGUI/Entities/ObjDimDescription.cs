using System.Runtime.InteropServices;

namespace DepthMapProcessorGUI.Entities
{
	[StructLayout(LayoutKind.Sequential)]
	public struct ObjDimDescription
	{
		public short Width;
		public short Height;
		public short Depth;
	}
}