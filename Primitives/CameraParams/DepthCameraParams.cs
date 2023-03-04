using Primitives.CameraParams;

namespace FrameProviders
{
	public class DepthCameraParams : CameraParams
	{
		public short MinDepth { get; }

		public short MaxDepth { get; }

		public DepthCameraParams(float fovX, float fovY, float focalLengthX, float focalLengthY, float principalX,
			float principalY, short minDepth, short maxDepth) : base(fovX, fovY, focalLengthX, focalLengthY, principalX, principalY)
		{
			MinDepth = minDepth;
			MaxDepth = maxDepth;
		}
	}
}