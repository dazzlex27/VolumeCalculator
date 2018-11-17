using System;

namespace Primitives
{
	public class CalculationResult
	{
		public DateTime CalculationTime { get; }

		public string ObjectCode { get; }

		public double ObjectWeight { get; }

		public int ObjectLength { get; }

		public int ObjectWidth { get; }

		public int ObjectHeight { get; }

		public long ObjectVolume { get; }

		public CalculationResult(DateTime calculationTime, string objectCode, double objectWeight, int objectLength,
			int objectWidth, int objectHeight, long objectVolume)
		{
			CalculationTime = calculationTime;
			ObjectCode = objectCode;
			ObjectWeight = objectWeight;
			ObjectLength = objectLength;
			ObjectWidth = objectWidth;
			ObjectHeight = objectHeight;
			ObjectVolume = objectVolume;
		}
	}
}
