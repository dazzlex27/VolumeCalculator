using Primitives;
using System.Windows.Media;

namespace VCClient.Utils
{
	internal class DashboardVmValueSet
	{
		public DashboardStatus DashStatus { get; }

		public bool CalculationInProgress { get; }

		public SolidColorBrush DashBrush { get; }

		public bool CalculationPending { get; }

		public string Message { get; }

		public DashboardVmValueSet(DashboardStatus dashStatus, bool calculationInProgress,
			SolidColorBrush dashBrush, bool calculationPending, string message)
		{
			DashStatus = dashStatus;
			CalculationInProgress = calculationInProgress;
			DashBrush = dashBrush;
			CalculationPending = calculationPending;
			Message = message;
		}
	}
}
