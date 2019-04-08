namespace DeviceIntegration.Scales
{
	public class ScaleMeasurementData
	{
		public MeasurementStatus Status { get; }

		public long WeightGr { get; }

		public ScaleMeasurementData(MeasurementStatus status, long weightGr)
		{
			Status = status;
			WeightGr = weightGr;
		}
	}
}