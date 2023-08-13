namespace VolumeCalculationRunner
{
	internal class AllCasesResult
	{
		public bool IsTestResultCorrect { get; }

		public string ErrorMessage { get; }

		public int TotalTestCount { get; private set; }

		public int FailedTestCount { get; private set; }

		public int SuccessfulTestCount => TotalTestCount - FailedTestCount;

		public float AvgLengthAccuracy { get; }

		public float AvgWidthAccuracy { get; }

		public float AvgHeightAccuracy { get; }

		public float AvgVolumeAccuracy { get; }

		public AllCasesResult(bool isTestResultCorrect, string errorMessage, int totalTestCount, int failedTestCount,
			float avgLengthAccuracy, float avgWidthAccuracy, float avgHeightAccuracy, float avgVolumeAccuracy)
		{
			IsTestResultCorrect = isTestResultCorrect;
			ErrorMessage = errorMessage;
			TotalTestCount = totalTestCount;
			FailedTestCount = failedTestCount;
			AvgLengthAccuracy = avgLengthAccuracy;
			AvgWidthAccuracy = avgWidthAccuracy;
			AvgHeightAccuracy = avgHeightAccuracy;
			AvgVolumeAccuracy = avgVolumeAccuracy;
		}
	}
}
