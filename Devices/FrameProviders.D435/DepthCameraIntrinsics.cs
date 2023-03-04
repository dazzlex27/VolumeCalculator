using System.Runtime.InteropServices;

namespace FrameProviders.D435
{
	[StructLayout(LayoutKind.Sequential)]
	public class DepthCameraIntrinsics
	{
		public float FocalLengthX;
		public float FocalLengthY;
		public float PrincipalPointX;
		public float PrincipalPointY;
	}
}
