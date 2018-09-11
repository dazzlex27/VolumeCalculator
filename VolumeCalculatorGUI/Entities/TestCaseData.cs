using Common;
using FrameSources;

namespace VolumeCalculatorGUI.Entities
{
	internal class TestCaseData
	{
		public string Casename { get; }
		public string SavingDirectory { get; }
		public int TimesToSave { get; }
		public ImageData Image { get; }
		public DepthMap Map { get; }
		public DeviceParams DeviceParams { get; }
		public short DistanceToFloor { get; }

		public TestCaseData(string casename, string savingDirectory, int timesToSave, ImageData image, DepthMap map, 
			DeviceParams deviceParams, short distanceToFloor)
		{
			Casename = casename;
			SavingDirectory = savingDirectory;
			TimesToSave = timesToSave;
			Image = image;
			Map = map;
			DeviceParams = deviceParams;
			DistanceToFloor = distanceToFloor;
		}
	}
}