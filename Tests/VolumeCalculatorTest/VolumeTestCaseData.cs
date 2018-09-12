using Common;

namespace VolumeCalculatorTest
{
	internal class VolumeTestCaseData
	{
		public string CaseName { get; }

		public string Description { get; }

		public DepthMap[] DepthMaps { get; }

		public int ObjWidth { get; }

		public int ObjHeight { get; }

		public int ObjDepth { get; }

		public short FloorDepth { get; }

		public short MinObjHeight { get; }

		public VolumeTestCaseData(string caseName, string description, DepthMap[] depthMaps, int objWidth, int objHeight, int objDepth, 
			short floorDepth, short minObjHeight)
		{
			CaseName = caseName;
			Description = description;
			DepthMaps = depthMaps;
			ObjWidth = objWidth;
			ObjHeight = objHeight;
			ObjDepth = objDepth;
			FloorDepth = floorDepth;
			MinObjHeight = minObjHeight;
		}
	}
}