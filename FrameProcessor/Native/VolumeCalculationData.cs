using System.Runtime.InteropServices;

namespace FrameProcessor.Native
{
	[StructLayout(LayoutKind.Sequential)]
	internal unsafe struct VolumeCalculationData
	{
		public DepthMap* DepthMap;
		public ColorImage* Image;
		public int SelectedAlgorithm;
		public long RangeMeterDistance;
		public bool SaveDebugData;
		public int CalculationNumber;
		public bool MaskMode;
	}
}