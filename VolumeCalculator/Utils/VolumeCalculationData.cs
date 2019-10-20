using FrameProcessor;

namespace VolumeCalculator.Utils
{
	internal class VolumeCalculationData
	{
		public DeviceSet DeviceSet { get; }

		public DepthMapProcessor DepthMapProcessor { get; }

		public int RequiredSampleCount { get; }

		public string Barcode { get; }

		public int CalculationIndex { get; }

		public bool Dm1AlgorithmEnabled { get; }

		public bool Dm2AlgorithmEnabled { get; }

		public bool RgbAlgorithmEnabled { get; }

		public string PhotosDirectoryPath { get; }

		public short CutOffDepth { get; }

		public VolumeCalculationData(DeviceSet deviceSet, DepthMapProcessor depthMapProcessor, int requiredSampleCount, 
			string barcode, int calculationIndex, bool dm1AlgorithmEnabled, bool dm2AlgorithmEnabled, 
			bool rgbAlgorithmEnabled, string photosDirectoryPath, short cutOffDepth)
		{
			DeviceSet = deviceSet;
			DepthMapProcessor = depthMapProcessor;
			RequiredSampleCount = requiredSampleCount;
			Barcode = barcode;
			CalculationIndex = calculationIndex;
			Dm1AlgorithmEnabled = dm1AlgorithmEnabled;
			Dm2AlgorithmEnabled = dm2AlgorithmEnabled;
			RgbAlgorithmEnabled = rgbAlgorithmEnabled;
			PhotosDirectoryPath = photosDirectoryPath;
			CutOffDepth = cutOffDepth;
		}
	}
}