namespace FrameSources
{
	public struct DeviceParams
	{
		public float FovX { get; }

		public float FovY { get; }

		public float FocalLengthX { get; }

		public float FocalLengthY { get; }

		public float PrincipalX { get; }

		public float PrincipalY { get; }

		public short MinDepth { get; }

		public short MaxDepth { get; }

		public DeviceParams(float fovX, float fovY, float focalLengthX, float focalLengthY, float principalX, float principalY,
			short minDepth, short maxDepth)
		{
			FovX = fovX;
			FovY = fovY;
			FocalLengthX = focalLengthX;
			FocalLengthY = focalLengthY;
			PrincipalX = principalX;
			PrincipalY = principalY;
			MinDepth = minDepth;
			MaxDepth = maxDepth;
		}
	}
}