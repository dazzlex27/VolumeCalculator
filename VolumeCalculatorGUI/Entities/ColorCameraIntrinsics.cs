using System.Runtime.InteropServices;

namespace VolumeCalculatorGUI.Entities
{
	[StructLayout(LayoutKind.Sequential)]
	public struct ColorCameraIntrinsics
	{
		public float FovX;
		public float FovY;
		public float FocalLengthX;
		public float FocalLengthY;
		public float PrincipalPointX;
		public float PrincipalPointY;
	}
}