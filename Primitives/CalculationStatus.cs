namespace Primitives
{
	public enum CalculationStatus
	{
		Ready,
		Pending,
		FailedToStart,
		Successful,
		BarcodeNotEntered,
		WeightNotStable,
		InProgress,
		AbortedByUser,
		TimedOut,
		CalculationError,
		FailedToSelectAlgorithm,
		ObjectNotFound,
		FailedToCloseFiles
	}
}