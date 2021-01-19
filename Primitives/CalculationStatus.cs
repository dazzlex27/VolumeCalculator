namespace Primitives
{
	public enum CalculationStatus
	{
		Undefined,
		Successful,
		BarcodeNotEntered,
		WeightNotStable,
		Running,
		AbortedByUser,
		TimedOut,
		CalculationError,
		FailedToSelectAlgorithm,
		ObjectNotFound,
		FailedToCloseFiles,
		WorkAreaNotReady
	}
}