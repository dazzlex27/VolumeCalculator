namespace FrameProcessor
{
	public class AlgorithmSelectionResult
	{
		public bool IsSelected { get; }
		
		public AlgorithmSelectionStatus Status { get; }

		public bool RangeMeterWasUsed { get; }

		public AlgorithmSelectionResult(bool isSelected, AlgorithmSelectionStatus status, bool rangeMeterWasUsed)
		{
			IsSelected = isSelected;
			Status = status;
			RangeMeterWasUsed = rangeMeterWasUsed;
		}
	}
}
