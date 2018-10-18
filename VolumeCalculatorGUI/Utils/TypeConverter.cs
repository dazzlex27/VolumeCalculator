using FrameProviders;
using VolumeCalculatorGUI.Entities;

namespace VolumeCalculatorGUI.Utils
{
	internal static class TypeConverter
	{
		public static ColorCameraIntrinsics ColorParamsToIntrinsics(ColorCameraParams colorCameraParams)
		{
			return new ColorCameraIntrinsics
			{
				FovX = colorCameraParams.FovX,
				FovY = colorCameraParams.FovY,
				FocalLengthX = colorCameraParams.FocalLengthX,
				FocalLengthY = colorCameraParams.FocalLengthY,
				PrincipalPointX = colorCameraParams.PrincipalPointX,
				PrincipalPointY = colorCameraParams.PrincipalPointY
			};
		}

		public static DepthCameraIntrinsics DepthParamsToIntrinsics(DepthCameraParams depthCameraParams)
		{
			return new DepthCameraIntrinsics
			{
				FovX = depthCameraParams.FovX,
				FovY = depthCameraParams.FovY,
				FocalLengthX = depthCameraParams.FocalLengthX,
				FocalLengthY = depthCameraParams.FocalLengthY,
				PrincipalPointX = depthCameraParams.PrincipalPointX,
				PrincipalPointY = depthCameraParams.PrincipalPointY,
				MinDepth = depthCameraParams.MinDepth,
				MaxDepth = depthCameraParams.MaxDepth
			};
		}
	}
}
