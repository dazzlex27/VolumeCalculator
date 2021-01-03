﻿using FrameProcessor;
using Primitives;

namespace VCServer
{
	public class VolumeCalculatorResultData
	{
		public ObjectVolumeData Result { get; }
		
		public CalculationStatus Status { get; }
		
		public ImageData ObjectPhoto { get; }
		
		public AlgorithmSelectionStatus LastAlgorithmUsed { get; }
		
		public bool WasRangeMeterUsed { get; }

		public VolumeCalculatorResultData(ObjectVolumeData result, CalculationStatus status, ImageData objectPhoto,
			AlgorithmSelectionStatus lastAlgorithmUsed, bool wasRangeMeterUsed)
		{
			Result = result;
			Status = status;
			ObjectPhoto = objectPhoto;
			LastAlgorithmUsed = lastAlgorithmUsed;
			WasRangeMeterUsed = wasRangeMeterUsed;
		}
	}
}