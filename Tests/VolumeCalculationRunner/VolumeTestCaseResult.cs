using VCServer.VolumeCalculation;

namespace VolumeCalculationRunner
{
	internal class VolumeTestCaseResult
	{
		public TestCaseResultType Status { get; }

		public float LengthAccuracy { get; } = float.MinValue;

		public float WidthAccuracy { get; } = float.MinValue;

		public float HeightAccuracy { get; } = float.MinValue;

		public float VolumeAccuracy { get; } = float.MinValue;

		public VolumeTestCaseResult(TestCaseResultType status, VolumeTestCaseData testCaseData = null,
			VolumeCalculationResultData resultData = null)
		{
			Status = status;

			if (status != TestCaseResultType.Success)
				return;

			var calcResult = resultData.Result;

			float lengthDelta = testCaseData.ObjLengthMm;
			LengthAccuracy = Math.Max(0, 1 - Math.Abs((calcResult.LengthMm - lengthDelta) / lengthDelta));

			float widthDelta = testCaseData.ObjWidthMm;
			WidthAccuracy = Math.Max(0, 1 - Math.Abs((calcResult.WidthMm - widthDelta) / widthDelta));

			float heightDelta = testCaseData.ObjHeightMm;
			HeightAccuracy = Math.Max(0, 1 - Math.Abs((calcResult.HeightMm - heightDelta) / heightDelta));

			var volumeDelta = (double)testCaseData.ObjLengthMm * testCaseData.ObjWidthMm * testCaseData.ObjHeightMm;
			var calculatedVolume = (double)calcResult.LengthMm * calcResult.WidthMm * calcResult.HeightMm;
			VolumeAccuracy = (float)(Math.Max(0, 1 - Math.Abs((calculatedVolume - volumeDelta) / volumeDelta)));

			if (LengthAccuracy < 0.01 || WidthAccuracy < 0.01 || HeightAccuracy < 0.01 || VolumeAccuracy < 0.01)
				Status = TestCaseResultType.TooInaccurate;
		}
	}
}
