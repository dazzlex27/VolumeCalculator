using Primitives;

namespace VolumeCalculator.Utils
{
	internal class StateData
	{
		public DashboardStatus DashStatus { get; }
		
		public string Message { get; }
		
		public CalculationStatus CalculationStatus { get; }

		public StateData(DashboardStatus dashStatus, string message, CalculationStatus calculationStatus)
		{
			DashStatus = dashStatus;
			Message = message;
			CalculationStatus = calculationStatus;
		}
	}
}