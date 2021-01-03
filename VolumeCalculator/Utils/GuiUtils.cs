using Primitives;

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
	}
}