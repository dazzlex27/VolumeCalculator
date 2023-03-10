using Primitives;
using Primitives.Calculation;
using System.Threading.Tasks;
using System;
using System.Windows.Media;
using VCServer;

namespace VCClient.Utils
{
	internal static class GuiUtils
	{
		public const string AppTitle = "VCClient";

		public static readonly string AppHeaderString =
			$@"{GlobalConstants.ManufacturerName} {AppTitle} {GlobalConstants.AppVersion}";

		public static string GetMessageFromCalculationStatus(CalculationStatus status)
		{
			var message = "";
			bool appendErrorPrefix = false;

			switch (status)
			{
				case CalculationStatus.CalculationError:
					message = "ошибка измерения";
					appendErrorPrefix = true;
					break;
				case CalculationStatus.TimedOut:
					message = "нарушена связь с устройством";
					appendErrorPrefix = true;
					break;
				case CalculationStatus.AbortedByUser:
					message = "измерение прервано";
					appendErrorPrefix = true;
					break;
				case CalculationStatus.FailedToSelectAlgorithm:
					message = "не удалось выбрать алгоритм";
					appendErrorPrefix = true;
					break;
				case CalculationStatus.ObjectNotFound:
					message = "объект не найден";
					appendErrorPrefix = true;
					break;
				case CalculationStatus.BarcodeNotEntered:
					message = "код не введен";
					appendErrorPrefix = true;
					break;
				case CalculationStatus.WeightNotStable:
					message = "вес нестабилен";
					appendErrorPrefix = true;
					break;
				case CalculationStatus.FailedToStart:
					message = "ошибка запуска";
					appendErrorPrefix = true;
					break;
				case CalculationStatus.FailedToCloseFiles:
					message = "открыт файл результатов";
					appendErrorPrefix = true;
					break;
				case CalculationStatus.Pending:
					message = "запущен автотаймер...";
					break;
				case CalculationStatus.InProgress:
					message = "выполняется измерение...";
					break;
				case CalculationStatus.Ready:
					message = "Готов к измерению";
					break;
				case CalculationStatus.Successful:
					message = "измерение завершено";
					break;
			}

			var prefix = appendErrorPrefix ? "Ошибка: " : "";

			return $"{prefix}{message}";
		}

		public static SolidColorBrush GetBrushFromDashboardStatus(DashboardStatus status)
		{
			switch (status)
			{
				case DashboardStatus.Ready:
					return new SolidColorBrush(Colors.Green);
				case DashboardStatus.InProgress:
					return new SolidColorBrush(Colors.DarkOrange);
				case DashboardStatus.Pending:
					return new SolidColorBrush(Colors.Blue);
				case DashboardStatus.Error:
					return new SolidColorBrush(Colors.Red);
				case DashboardStatus.Finished:
					return new SolidColorBrush(Colors.DarkGreen);
				default:
					return new SolidColorBrush(Colors.Black);
			}
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
