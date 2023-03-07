namespace VCServer.VolumeCalculation
{
	internal sealed class VolumeCalculationData
	{
		public int RequiredSampleCount { get; }

		public string Barcode { get; }

		public int CalculationIndex { get; }

		public bool Dm1AlgorithmEnabled { get; }

		public bool Dm2AlgorithmEnabled { get; }

		public bool RgbAlgorithmEnabled { get; }

		public string PhotosDirectoryPath { get; }

		public short FloorDepth { get; }

		public short CutOffDepth { get; }

		public int RangeMeterCorrectionValue { get; }

		public VolumeCalculationData(int requiredSampleCount,
			string barcode, int calculationIndex, bool dm1AlgorithmEnabled, bool dm2AlgorithmEnabled,
			bool rgbAlgorithmEnabled, string photosDirectoryPath, short floorDepth, short cutOffDepth,
			int rangeMeterCorrectionValue)
		{
			RequiredSampleCount = requiredSampleCount;
			Barcode = barcode;
			CalculationIndex = calculationIndex;
			Dm1AlgorithmEnabled = dm1AlgorithmEnabled;
			Dm2AlgorithmEnabled = dm2AlgorithmEnabled;
			RgbAlgorithmEnabled = rgbAlgorithmEnabled;
			PhotosDirectoryPath = photosDirectoryPath;
			FloorDepth = floorDepth;
			CutOffDepth = cutOffDepth;
			RangeMeterCorrectionValue = rangeMeterCorrectionValue;
		}
	}
}
