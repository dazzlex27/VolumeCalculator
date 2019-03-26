namespace DeviceIntegration.Scales
{
	public class ScaleMeasurementData
	{
		public MeasurementStatus Status { get; }

		public int WeightGr { get; }

		public ScaleMeasurementData(MeasurementStatus status, int weightGr)
		{
			Status = status;
			WeightGr = weightGr;
		}
	}
}