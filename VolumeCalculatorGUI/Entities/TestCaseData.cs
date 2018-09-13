using Common;
using FrameSources;

namespace VolumeCalculatorGUI.Entities
{
	internal class TestCaseData
	{
		public string Casename { get; }
		public string Description { get; }
		public string SavingDirectory { get; }
		public string ObjWidth { get; }
		public string ObjHeight { get; }
		public string ObjDepth { get; }
		public int TimesToSave { get; }
		public ImageData Image { get; }
		public DepthMap Map { get; }
		public DeviceParams DeviceParams { get; }
		public short DistanceToFloor { get; }
		public short MinObjHeight { get; }

		public TestCaseData(string casename, string description, string savingDirectory, string objWidth, string objHeight, 
			string objDepth, int timesToSave, ImageData image, DepthMap map, DeviceParams deviceParams, short distanceToFloor,
			short minObjHeight)
		{
			Casename = casename;
			Description = description;
			SavingDirectory = savingDirectory;
			ObjWidth = objWidth;
			ObjHeight = objHeight;
			ObjDepth = objDepth;
			TimesToSave = timesToSave;
			Image = image;
			Map = map;
			DeviceParams = deviceParams;
			DistanceToFloor = distanceToFloor;
			MinObjHeight = minObjHeight;
		}
	}
}