using System;
using Primitives;
using Primitives.Calculation;

namespace VCServer
{
	public static class StatusUtils
	{
		public static DashboardStatus GetDashboardStatus(CalculationStatus status)
		{
			DashboardStatus dashStatus;

			switch (status)
			{
				case CalculationStatus.Successful:
					dashStatus = DashboardStatus.Finished;
					break;
				case CalculationStatus.InProgress:
					dashStatus = DashboardStatus.InProgress;
					break;
				case CalculationStatus.Ready:
					dashStatus = DashboardStatus.Ready;
					break;
				case CalculationStatus.Pending:
					dashStatus = DashboardStatus.Pending;
					break;
				case CalculationStatus.CalculationError:
				case CalculationStatus.AbortedByUser:
				case CalculationStatus.TimedOut:
				case CalculationStatus.BarcodeNotEntered:
				case CalculationStatus.FailedToStart:
				case CalculationStatus.ObjectNotFound:
				case CalculationStatus.WeightNotStable:
				case CalculationStatus.FailedToCloseFiles:
				case CalculationStatus.FailedToSelectAlgorithm:
					dashStatus = DashboardStatus.Error;
					break;
				default:
					throw new ArgumentOutOfRangeException(nameof(status), status, "failed to parse status");
			}

			return dashStatus;
		}
	}
}
