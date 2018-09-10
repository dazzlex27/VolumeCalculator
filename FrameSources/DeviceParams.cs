namespace FrameSources
{
	public struct DeviceParams
	{
		public float FovX { get; }

		public float FovY { get; }

		public short MinDepth { get; }

		public short MaxDepth { get; }

		public DeviceParams(float fovX, float fovY, short minDepth, short maxDepth)
		{
			FovX = fovX;
			FovY = fovY;
			MinDepth = minDepth;
			MaxDepth = maxDepth;
		}
	}
}