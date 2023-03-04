using Primitives.CameraParams;

namespace FrameProviders
{
	public class ColorCameraParams : CameraParams
	{
		public ColorCameraParams(float fovX, float fovY, float focalLengthX, float focalLengthY, float principalX, float principalY) 
			: base(fovX, fovY, focalLengthX, focalLengthY, principalX, principalY)
		{
		}
	}
}