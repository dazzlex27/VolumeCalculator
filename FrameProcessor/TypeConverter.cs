using FrameProcessor.Native;
using FrameProviders;

namespace FrameProcessor
{
	internal static class TypeConverter
	{
		public static CameraIntrinsics ColorParamsToIntrinsics(ColorCameraParams colorCameraParams)
		{
			if (colorCameraParams == null)
				return new CameraIntrinsics();

			return new CameraIntrinsics
			{
				FovX = colorCameraParams.FovX,
				FovY = colorCameraParams.FovY,
				FocalLengthX = colorCameraParams.FocalLengthX,
				FocalLengthY = colorCameraParams.FocalLengthY,
				PrincipalPointX = colorCameraParams.PrincipalPointX,
				PrincipalPointY = colorCameraParams.PrincipalPointY
			};
		}

		public static CameraIntrinsics DepthParamsToIntrinsics(DepthCameraParams depthCameraParams)
		{
			if (depthCameraParams == null)
				return new CameraIntrinsics();

			return new CameraIntrinsics
			{
				FovX = depthCameraParams.FovX,
				FovY = depthCameraParams.FovY,
				FocalLengthX = depthCameraParams.FocalLengthX,
				FocalLengthY = depthCameraParams.FocalLengthY,
				PrincipalPointX = depthCameraParams.PrincipalPointX,
				PrincipalPointY = depthCameraParams.PrincipalPointY
			};
		}
	}
}