using System;

namespace Primitives
{
	public class CalculationResult
	{
		public DateTime CalculationTime { get; }

		public string ObjectCode { get; }

		public double ObjectWeight { get; }

		public uint UnitCount { get; }

		public int ObjectLength { get; }

		public int ObjectWidth { get; }

		public int ObjectHeight { get; }

		public long ObjectVolume { get; }

		public string CalculationComment { get; }

		public CalculationResult(DateTime calculationTime, string objectCode, double objectWeight, uint unitCount,
			int objectLength, int objectWidth, int objectHeight, long objectVolume, string calculationComment)
		{
			CalculationTime = calculationTime;
			ObjectCode = objectCode;
			ObjectWeight = objectWeight;
			UnitCount = unitCount;
			ObjectLength = objectLength;
			ObjectWidth = objectWidth;
			ObjectHeight = objectHeight;
			ObjectVolume = objectVolume;
			CalculationComment = calculationComment;
		}
	}
}