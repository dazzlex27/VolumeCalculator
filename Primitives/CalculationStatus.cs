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
		Error,
		FailedToSelectAlgorithm,
		ObjectNotFound,
		FailedToCloseFiles,
		WorkAreaNotReady
	}
}