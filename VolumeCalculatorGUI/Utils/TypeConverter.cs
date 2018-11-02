using FrameProviders;
using VolumeCalculatorGUI.Entities;
using VolumeCalculatorGUI.Entities.Native;

namespace VolumeCalculatorGUI.Utils
{
	internal static class TypeConverter
	{
		public static ColorCameraIntrinsics ColorParamsToIntrinsics(ColorCameraParams colorCameraParams)
		{
			if (colorCameraParams == null)
				return new ColorCameraIntrinsics();

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
			if (depthCameraParams == null)
				return new DepthCameraIntrinsics();

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
