namespace Primitives
{
	public class CalculationResultData
	{
		public CalculationResult Result { get; }

		public CalculationStatus Status { get; }

		public CalculationResultData(CalculationResult result, CalculationStatus status)
		{
			Result = result;
			Status = status;
		}
	}
}