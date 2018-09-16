using Common;
using FrameProviders;

namespace VolumeCalculatorGUI.Entities
{
	internal class TestCaseData
	{
		public TestCaseBasicInfo BasicInfo { get; }
		public ImageData Image { get; }
		public DepthMap Map { get; }
		public DeviceParams DeviceParams { get; }
		public ApplicationSettings Settings { get; }

		public TestCaseData(TestCaseBasicInfo basicInfo, ImageData image, DepthMap map, DeviceParams deviceParams,
			ApplicationSettings settings)
		{
			BasicInfo = basicInfo;
			Image = image;
			Map = map;
			DeviceParams = deviceParams;
			Settings = settings;
		}
	}
}