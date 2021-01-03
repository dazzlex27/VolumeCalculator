using System.Runtime.InteropServices;

namespace FrameProcessor.Native
{
	[StructLayout(LayoutKind.Sequential)]
	internal unsafe struct VolumeCalculationData
	{
		public DepthMap* DepthMap;
		public ColorImage* ColorImage;
		public AlgorithmSelectionStatus SelectedAlgorithm;
		public short CalculatedDistance;
	}
}