namespace Primitives
{
	public class CalculationResultData
	{
		public CalculationResult Result { get; }

		public CalculationStatus Status { get; }

		public ImageData ObjectPhoto { get; }

		public CalculationResultData(CalculationResult result, CalculationStatus status, ImageData objectPhoto)
		{
			Result = result;
			Status = status;
			ObjectPhoto = objectPhoto;
		}
	}
}