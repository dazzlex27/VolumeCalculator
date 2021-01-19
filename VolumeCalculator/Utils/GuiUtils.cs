using Primitives;

namespace VolumeCalculator.Utils
{
	internal static class GuiUtils
	{
		public static string GetErrorMessageFromCalculationStatus(CalculationStatus status)
		{
			var message = "";

			switch (status)
			{
				case CalculationStatus.CalculationError:
					message = "ошибка измерения";
					break;
				case CalculationStatus.TimedOut:
					message = "нарушена связь с устройством";
					break;
				case CalculationStatus.AbortedByUser:
					message = "измерение прервано";
					break;
				case CalculationStatus.FailedToSelectAlgorithm:
					message = "не удалось выбрать алгоритм";
					break;
				case CalculationStatus.ObjectNotFound:
					message = "объект не найден";
					break;
				case CalculationStatus.BarcodeNotEntered:
					message = "код не введен";
					break;
				case CalculationStatus.WeightNotStable:
					message = "вес нестабилен";
					break;
				case CalculationStatus.FailedToStart:
					message = "ошибка запуска";
					break;
				case CalculationStatus.FailedToCloseFiles:
					message = "открыт файл результатов";
					break;
			}

			return message;
		}
	}
}