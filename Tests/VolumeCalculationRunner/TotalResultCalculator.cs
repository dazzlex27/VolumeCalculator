namespace VolumeCalculationRunner
{
	internal class TotalResultCalculator
	{
		private float _avgLengthAccuracy;
		private float _avgWidthAccuracy;
		private float _avgHeightAccuracy;
		private float _avgVolumeAccuracy;

		public int TotalTestCount { get; private set; }

		public int FailedTestCount { get; private set; }

		public int SuccessfullTestCount => TotalTestCount - FailedTestCount;

		public TotalResultCalculator()
		{
			_avgLengthAccuracy = 1;
			_avgWidthAccuracy = 1;
			_avgHeightAccuracy = 1;
			_avgVolumeAccuracy = 1;
		}

		public void AddTestCaseResult(VolumeTestCaseResult caseResult)
		{
			if (caseResult == null)
				return;

			TotalTestCount++;

			if (caseResult.Status != TestCaseResultType.Success)
				FailedTestCount++;
			else
			{
				var testRatio = 1.0f / SuccessfullTestCount;

				_avgLengthAccuracy = _avgLengthAccuracy * (1 - testRatio) + caseResult.LengthAccuracy * testRatio;
				_avgWidthAccuracy = _avgWidthAccuracy * (1 - testRatio) + caseResult.WidthAccuracy * testRatio;
				_avgHeightAccuracy = _avgHeightAccuracy * (1 - testRatio) + caseResult.HeightAccuracy * testRatio;
				_avgVolumeAccuracy = _avgVolumeAccuracy * (1 - testRatio) + caseResult.VolumeAccuracy * testRatio;
			}
		}

		public AllCasesResult GetAllCasesResult()
		{
			var testSuccessFull = true;
			var errorMessage = "";

			if (_avgVolumeAccuracy < 0.05)
			{
				errorMessage = $"Total accuracy is 0.0!";
				testSuccessFull = false;
			}

			if ((float)FailedTestCount / TotalTestCount > 0.3)
			{
				errorMessage = $"Too many failed tests! ( >50% )";
				testSuccessFull = false;
			}

			if (!float.IsNormal(_avgLengthAccuracy) || !float.IsNormal(_avgWidthAccuracy) 
				|| !float.IsNormal(_avgHeightAccuracy) || !float.IsNormal(_avgVolumeAccuracy))
			{
				errorMessage = $"Incorrect accuracy values!";
				testSuccessFull = false;
			}

			return new AllCasesResult(testSuccessFull, errorMessage, TotalTestCount, FailedTestCount,
				_avgLengthAccuracy, _avgWidthAccuracy, _avgHeightAccuracy, _avgVolumeAccuracy);
		}
	}
}
