using System;

namespace Primitives
{
	public class CalculationResult
	{
		public CalculationStatus Status { get; }

		public DateTime CalculationTime { get; }

		public string ObjectCode { get; }

		public double ObjectWeightKg { get; }

		public uint UnitCount { get; }

		public int ObjectLengthMm { get; }

		public int ObjectWidthMm { get; }

		public int ObjectHeightMm { get; }

		public double ObjectVolumeMm { get; }

		public string CalculationComment { get; }

		public CalculationResult(CalculationStatus status, DateTime calculationTime, string objectCode,
			double objectWeightKg, uint unitCount, int objectLengthMm, int objectWidthMm, int objectHeightMm,
			double objectVolumeMm, string calculationComment)
		{
			Status = status;
			CalculationTime = calculationTime;
			ObjectCode = objectCode;
			ObjectWeightKg = objectWeightKg;
			UnitCount = unitCount;
			ObjectLengthMm = objectLengthMm;
			ObjectWidthMm = objectWidthMm;
			ObjectHeightMm = objectHeightMm;
			ObjectVolumeMm = objectVolumeMm;
			CalculationComment = calculationComment;
		}
	}
}