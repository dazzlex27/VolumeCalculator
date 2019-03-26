using System;

namespace Primitives
{
	public class CalculationResult
	{
		public DateTime CalculationTime { get; }

		public string ObjectCode { get; }

		public int ObjectWeightGr { get; }

		public uint UnitCount { get; }

		public int ObjectLengthMm { get; }

		public int ObjectWidthMm { get; }

		public int ObjectHeightMm { get; }

		public double ObjectVolumeMm { get; }

		public string CalculationComment { get; }

		public CalculationResult(DateTime calculationTime, string objectCode, int objectWeightGr, uint unitCount, 
			int objectLengthMm, int objectWidthMm, int objectHeightMm, double objectVolumeMm, string calculationComment)
		{
			CalculationTime = calculationTime;
			ObjectCode = objectCode;
			ObjectWeightGr= objectWeightGr;
			UnitCount = unitCount;
			ObjectLengthMm = objectLengthMm;
			ObjectWidthMm = objectWidthMm;
			ObjectHeightMm = objectHeightMm;
			ObjectVolumeMm = objectVolumeMm;
			CalculationComment = calculationComment;
		}
	}
}