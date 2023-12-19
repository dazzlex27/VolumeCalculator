using Primitives;
using Primitives.Calculation;
using System;
using System.Windows.Media;
using VCServer;
using GuiCommon.Localization;

namespace VCClient.Utils
{
	internal static class GuiUtils
	{
		public const string AppTitle = "VCClient";

		public static readonly string AppHeaderString =
			$@"{GlobalConstants.ManufacturerName} {AppTitle} {GlobalConstants.AppVersion}";

		public static string GetMessageFromCalculationStatus(CalculationStatus status)
		{
			return status switch
			{
				CalculationStatus.CalculationError => TranslationManager.Instance.Translate("CalculationStatus.CalculationError") as string,
				CalculationStatus.TimedOut => TranslationManager.Instance.Translate("CalculationStatus.TimedOut") as string,
				CalculationStatus.AbortedByUser => TranslationManager.Instance.Translate("CalculationStatus.AbortedByUser") as string,
				CalculationStatus.FailedToSelectAlgorithm => TranslationManager.Instance.Translate("CalculationStatus.FailedToSelectAlgorithm") as string,
				CalculationStatus.ObjectNotFound => TranslationManager.Instance.Translate("CalculationStatus.ObjectNotFound") as string,
				CalculationStatus.BarcodeNotEntered => TranslationManager.Instance.Translate("CalculationStatus.BarcodeNotEntered") as string,
				CalculationStatus.WeightNotStable => TranslationManager.Instance.Translate("CalculationStatus.WeightNotStable") as string,
				CalculationStatus.FailedToStart => TranslationManager.Instance.Translate("CalculationStatus.FailedToStart") as string,
				CalculationStatus.FailedToCloseFiles => TranslationManager.Instance.Translate("CalculationStatus.FailedToCloseFiles") as string,
				CalculationStatus.Pending => TranslationManager.Instance.Translate("CalculationStatus.Pending") as string,
				CalculationStatus.InProgress => TranslationManager.Instance.Translate("CalculationStatus.InProgress") as string,
				CalculationStatus.Ready => TranslationManager.Instance.Translate("CalculationStatus.Ready") as string,
				CalculationStatus.Successful => TranslationManager.Instance.Translate("CalculationStatus.Successful") as string,
				_ => "#Unknown status#",
			};
		}

		public static SolidColorBrush GetBrushFromDashboardStatus(DashboardStatus status)
		{
			return status switch
			{
				DashboardStatus.Ready => new SolidColorBrush(Colors.Green),
				DashboardStatus.InProgress => new SolidColorBrush(Colors.DarkOrange),
				DashboardStatus.Pending => new SolidColorBrush(Colors.Blue),
				DashboardStatus.Error => new SolidColorBrush(Colors.Red),
				DashboardStatus.Finished => new SolidColorBrush(Colors.DarkGreen),
				_ => new SolidColorBrush(Colors.Black),
			};
		}

		public static DashboardVmValueSet GetDashboardValuesFromCaculationStatus(CalculationStatus status)
		{
			var dashStatus = StatusUtils.GetDashboardStatus(status);
			var dashBrush = GetBrushFromDashboardStatus(dashStatus);
			var message = GetMessageFromCalculationStatus(status);

			return dashStatus switch
			{
				DashboardStatus.InProgress => new DashboardVmValueSet(dashStatus, true, dashBrush, false, message),
				DashboardStatus.Ready => new DashboardVmValueSet(dashStatus, false, dashBrush, false, message),
				DashboardStatus.Pending => new DashboardVmValueSet(dashStatus, false, dashBrush, true, message),
				DashboardStatus.Finished => new DashboardVmValueSet(dashStatus, false, dashBrush, false, message),
				DashboardStatus.Error => new DashboardVmValueSet(dashStatus, false, dashBrush, false, message),
				_ => throw new ArgumentOutOfRangeException(nameof(status), status, null),
			};
		}
	}
}
