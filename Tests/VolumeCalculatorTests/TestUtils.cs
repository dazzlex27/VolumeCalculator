using FrameProviders;

namespace VolumeCalculatorTests
{
	internal class TestUtils
	{
		public static ColorCameraParams GetDummyColorCameraParams()
		{
			return new ColorCameraParams(1, 1, 1, 1, 1, 1);
		}

		public static DepthCameraParams GetDummyDepthCameraParams()
		{
			return new DepthCameraParams(1, 1, 1, 1, 1, 1, 1, 1);
		}
	}
}