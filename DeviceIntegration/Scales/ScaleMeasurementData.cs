namespace DeviceIntegration.Scales
{
	public class ScaleMeasurementData
	{
		public MeasurementStatus Status { get; }

		public double WeightKg { get; }

		public ScaleMeasurementData(MeasurementStatus status, double weightKg)
		{
			Status = status;
			WeightKg = weightKg;
		}
	}
}