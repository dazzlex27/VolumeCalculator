using System;
using Primitives;
using Primitives.Logging;
using VCServer;

namespace VolumeCalculator.Utils
{
	internal static class GuiUtils
	{
		public static CalculationStatus GetCalculationStatus(ErrorCode preConditionEvaluationResult,
			out string errorMessage)
		{
			var status = CalculationStatus.Undefined;
			errorMessage = "";

			switch (preConditionEvaluationResult)
			{
				case ErrorCode.BarcodeNotEntered:
					status = CalculationStatus.BarcodeNotEntered;
					errorMessage = "Введите код объекта";
					break;
				case ErrorCode.FileHandleOpen:
					status = CalculationStatus.FailedToCloseFiles;
					errorMessage = "Открыт файл результатов";
					break;
				case ErrorCode.WeightNotStable:
					status = CalculationStatus.WeightNotStable;
					errorMessage = "Вес нестабилен";
					break;
			}

			return status;
		}

		public static StateData GetDashStatusAfterCalculation(CalculationResult result, CalculationStatus status, 
			ILogger logger)
		{
			var message = "";
			DashboardStatus dashboardStatus;

			switch (status)
			{
				case CalculationStatus.Successful:
				{
					var info = $"L={result.ObjectLengthMm} W={result.ObjectWidthMm} H={result.ObjectHeightMm}";
					logger.LogInfo($"Completed a volume check: {info}");
					dashboardStatus = DashboardStatus.Finished;
					break;
				}
				case CalculationStatus.CalculationError:
				{
					logger.LogError("Volume calculation finished with errors");
					message = "ошибка измерения";
					dashboardStatus = DashboardStatus.Error;
					break;
				}
				case CalculationStatus.TimedOut:
				{
					logger.LogError("Failed to acquire enough samples for volume calculation");
					message = "нарушена связь с устройством";
					dashboardStatus = DashboardStatus.Error;
					break;
				}
				case CalculationStatus.Undefined:
				{
					logger.LogError("undefined error occured");
					message = "неизвестная ошибка";
					dashboardStatus = DashboardStatus.Error;
					break;
				}
				case CalculationStatus.AbortedByUser:
				{
					logger.LogError("Volume calculation was aborted");
					message = "измерение прервано";
					dashboardStatus = DashboardStatus.Error;
					break;
				}
				case CalculationStatus.FailedToSelectAlgorithm:
				{
					logger.LogError("Failed to select algorithm");
					message = "не удалось выбрать алгоритм";
					dashboardStatus = DashboardStatus.Error;
					break;
				}
				case CalculationStatus.ObjectNotFound:
				{
					logger.LogError("Object was not found");
					message = "объект не найден";
					dashboardStatus = DashboardStatus.Error;
					break;
				}
				default:
					throw new ArgumentOutOfRangeException(nameof(status), status,
						@"Failed to resolve failed calculation status");
			}

			return new StateData(dashboardStatus, message, status);
		}
	}
}