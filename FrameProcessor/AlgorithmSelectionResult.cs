namespace FrameProcessor
{
	public class AlgorithmSelectionResult
	{
		public AlgorithmSelectionStatus Status { get; }
		public bool RangeMeterWasUsed { get; }

		public AlgorithmSelectionResult(AlgorithmSelectionStatus status, bool rangeMeterWasUsed)
		{
			Status = status;
			RangeMeterWasUsed = rangeMeterWasUsed;
		}
	}
}