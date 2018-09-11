namespace FrameSources
{
	public struct DeviceParams
	{
		public float FovX { get; }

		public float FovY { get; }

		public float FocalLengthX { get; }

		public float FocalLengthY { get; }

		public short MinDepth { get; }

		public short MaxDepth { get; }

		public DeviceParams(float fovX, float fovY, float focalLengthX, float focalLengthY, short minDepth, 
			short maxDepth)
		{
			FovX = fovX;
			FovY = fovY;
			FocalLengthX = focalLengthX;
			FocalLengthY = focalLengthY;
			MinDepth = minDepth;
			MaxDepth = maxDepth;
		}
	}
}