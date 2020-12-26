namespace Primitives
{
	public enum CalculationStatus
	{
		Undefined,
		Successful,
		BarcodeNotEntered,
		Running,
		AbortedByUser,
		TimedOut,
		Error,
		FailedToSelectAlgorithm,
		ObjectNotFound
	}
}