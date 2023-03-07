using System.Runtime.InteropServices;

namespace FrameProcessor.Native
{
	[StructLayout(LayoutKind.Sequential)]
	internal struct NativeAlgorithmSelectionResult
	{
		public AlgorithmSelectionStatus Status;
		public int RangeMeterWasUsed;
	}
}
