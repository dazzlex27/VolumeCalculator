namespace Primitives
{
	public enum CalculationStatus
	{
		Undefined,
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