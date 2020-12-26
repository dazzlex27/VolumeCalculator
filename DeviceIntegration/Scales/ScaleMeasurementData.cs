namespace DeviceIntegration.Scales
{
	public class ScaleMeasurementData
	{
		public MeasurementStatus Status { get; }

		public double WeightGr { get; }

		public ScaleMeasurementData(MeasurementStatus status, double weightGr)
		{
			Status = status;
			WeightGr = weightGr;
		}
	}
}