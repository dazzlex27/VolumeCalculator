using System.Runtime.InteropServices;

namespace FrameProcessor.Native
{
	[StructLayout(LayoutKind.Sequential)]
	public struct CameraIntrinsics
	{
		public float FovX;
		public float FovY;
		public float FocalLengthX;
		public float FocalLengthY;
		public float PrincipalPointX;
		public float PrincipalPointY;
	}
}