using System.Runtime.InteropServices;

namespace VolumeCalculatorGUI.Entities.Native
{
	[StructLayout(LayoutKind.Sequential)]
	public struct DepthCameraIntrinsics
	{
		public float FovX;
		public float FovY;
		public float FocalLengthX;
		public float FocalLengthY;
		public float PrincipalPointX;
		public float PrincipalPointY;
		public short MinDepth;
		public short MaxDepth;
	}
}