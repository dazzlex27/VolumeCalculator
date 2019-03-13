namespace Primitives
{
	public enum CalculationStatus
	{
		Undefined,
		Ready,
		Sucessful,
		BarcodeNotEntered,
		Running,
		AbortedByUser,
		TimedOut,
		Error,
		FailedToSelectAlgorithm,
		ObjectNotFound
	}
}