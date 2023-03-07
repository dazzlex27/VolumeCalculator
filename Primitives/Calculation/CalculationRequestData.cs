namespace Primitives.Calculation
{
	public class CalculationRequestData
	{
		public string Barcode { get; }

		public uint UnitCount { get; }

		public string Comment { get; }

		public CalculationRequestData(string barcode, uint unitCount, string comment)
		{
			Barcode = barcode;
			UnitCount = unitCount;
			Comment = comment;
		}
	}
}
