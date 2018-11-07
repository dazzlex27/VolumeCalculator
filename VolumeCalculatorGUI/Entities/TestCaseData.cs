using FrameProviders;
using Primitives;

namespace VolumeCalculatorGUI.Entities
{
	internal class TestCaseData
	{
		public TestCaseBasicInfo BasicInfo { get; }
		public ImageData Image { get; }
		public DepthMap Map { get; }
		public DepthCameraParams DepthCameraParams { get; }
		public ApplicationSettings Settings { get; }

		public TestCaseData(TestCaseBasicInfo basicInfo, ImageData image, DepthMap map, DepthCameraParams depathCameraParams,
			ApplicationSettings settings)
		{
			BasicInfo = basicInfo;
			Image = image;
			Map = map;
			DepthCameraParams = depathCameraParams;
			Settings = settings;
		}
	}
}