using Primitives;

namespace FrameProcessor
{
	public class AlgorithmSelectionData
	{
		public DepthMap DepthMap { get; }
		
		public ImageData Image { get; }
		
		public short CalculatedDistance { get; }
		
		public bool Dm1Enabled { get; }
		
		public bool Dm2Enabled { get; }
		
		public bool RgbEnabled { get; }
		
		public string DebugFileName { get; }

		public AlgorithmSelectionData(DepthMap depthMap, ImageData image, short calculatedDistance,
			bool dm1Enabled, bool dm2Enabled, bool rgbEnabled, string debugFileName)
		{
			DepthMap = depthMap;
			Image = image;
			CalculatedDistance = calculatedDistance;
			Dm1Enabled = dm1Enabled;
			Dm2Enabled = dm2Enabled;
			RgbEnabled = rgbEnabled;
			DebugFileName = debugFileName;
		}
	}
}
