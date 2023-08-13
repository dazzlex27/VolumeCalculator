using Primitives;

namespace VolumeCalculationRunner
{
	internal class VolumeTestCaseData
	{
		public string CaseName { get; }

		public string Description { get; }

		public IEnumerable<DepthMap> DepthMaps { get; }

		public ImageData Image { get; }

		public int ObjLengthMm { get; }

		public int ObjWidthMm { get; }

		public int ObjHeightMm { get; }

		public short FloorDepthMm { get; }

		public short MinObjHeightMm { get; }

		public VolumeTestCaseData(string caseName, string description, IEnumerable<DepthMap> depthMaps, ImageData image,
			int objLengthMm, int objWidthMm, int objHeightMm, short floorDepthMm, short minObjHeightMm)
		{
			CaseName = caseName;
			Description = description;
			DepthMaps = depthMaps;
			Image = image;
			ObjWidthMm = objWidthMm;
			ObjHeightMm = objHeightMm;
			ObjLengthMm = objLengthMm;
			FloorDepthMm = floorDepthMm;
			MinObjHeightMm = minObjHeightMm;
		}
	}
}
