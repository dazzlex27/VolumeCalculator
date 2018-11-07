using Primitives;

namespace VolumeCalculationRunner
{
	internal class VolumeTestCaseData
	{
		public string CaseName { get; }

		public string Description { get; }

		public DepthMap[] DepthMaps { get; }

		public int ObjLength { get; }

		public int ObjWidth { get; }

		public int ObjHeight { get; }

		public short FloorDepth { get; }

		public short MinObjHeight { get; }

		public VolumeTestCaseData(string caseName, string description, DepthMap[] depthMaps, int objLength, int objWidth, int objHeight,  
			short floorDepth, short minObjHeight)
		{
			CaseName = caseName;
			Description = description;
			DepthMaps = depthMaps;
			ObjWidth = objWidth;
			ObjHeight = objHeight;
			ObjLength = objLength;
			FloorDepth = floorDepth;
			MinObjHeight = minObjHeight;
		}
	}
}