using System.Runtime.InteropServices;

namespace FrameProcessor.Native
{
	[StructLayout(LayoutKind.Sequential)]
	internal unsafe struct NativeAlgorithmSelectionData
	{
		public DepthMap* DepthMap;
		public ColorImage* ColorImage;
		public short CalculatedDistance;
		[MarshalAs(UnmanagedType.I1)]
		public bool Dm1Enabled;
		[MarshalAs(UnmanagedType.I1)]
		public bool Dm2Enabled;
		[MarshalAs(UnmanagedType.I1)]
		public bool RgbEnabled;
		[MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)]
		public string DebugFileName;
	}
}