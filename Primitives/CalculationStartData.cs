using System;

namespace Primitives
{
	public class CalculationStartData
	{
		public DateTime CalculationTime { get; }
		
		public string Barcode { get; }

		public uint UnitCount { get; }

		public string Comment { get; }
		
		public double WeightGr { get; }
		
		public short PalletHeightSubtractionMm { get; }
		
		public double PalletWeightSubtractionGr { get; }

		public CalculationStartData(DateTime calculationTime, string barcode, uint unitCount, string comment,
			double weightGr, short palletHeightSubtractionMm, double palletWeightSubtractionGr)
		{
			CalculationTime = calculationTime;
			Barcode = barcode;
			UnitCount = unitCount;
			Comment = comment;
			WeightGr = weightGr;
			PalletHeightSubtractionMm = palletHeightSubtractionMm;
			PalletWeightSubtractionGr = palletWeightSubtractionGr;
		}
	}
}