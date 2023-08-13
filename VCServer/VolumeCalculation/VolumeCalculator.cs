using System;
using System.Collections.Generic;
using System.Linq;
using FrameProcessor;
using Primitives;
using Primitives.Calculation;
using Primitives.Logging;

namespace VCServer.VolumeCalculation
{
	internal sealed class VolumeCalculator
	{
		private readonly ILogger _logger;
		private readonly DepthMapProcessor _processor;

		public VolumeCalculator(ILogger logger, DepthMapProcessor processor)
		{
			_logger = logger;
			_processor = processor;
		}

		public VolumeCalculationResultData Calculate(IReadOnlyList<ImageData> images, IReadOnlyList<DepthMap> depthMaps,
			VolumeCalculationData data, short calculatedDistance)
		{
			if (images.Count == 0 || depthMaps.Count == 0)
				throw new ArgumentException("not enough input frames");

			var firstImage = images[0];

			var algorithmSelectionData = new AlgorithmSelectionData(depthMaps[0], firstImage, calculatedDistance,
				data.Dm1AlgorithmEnabled, data.Dm2AlgorithmEnabled, data.RgbAlgorithmEnabled, data.PhotosDirectoryPath);

			var algorithmSelectionResult = _processor.SelectAlgorithm(algorithmSelectionData);
			var algorithm = algorithmSelectionResult.Status;
			var rangeMeterWasUsed = algorithmSelectionResult.RangeMeterWasUsed;
			if (!algorithmSelectionResult.IsSelected)
				return new VolumeCalculationResultData(null, CalculationStatus.FailedToSelectAlgorithm,
					firstImage, algorithm, algorithmSelectionResult.RangeMeterWasUsed);

			var results = new List<ObjectVolumeData>();

			for (var i = 0; i < data.RequiredSampleCount; i++)
			{
				var currentResult = _processor.CalculateVolume(depthMaps[i], images[i], calculatedDistance, algorithm);
				results.Add(currentResult);
			}

			var aggregatedResult = AggregateCalculationsData(results);
			var resultStatus = aggregatedResult != null
				? CalculationStatus.Successful
				: CalculationStatus.CalculationError;

			return new VolumeCalculationResultData(aggregatedResult, resultStatus, firstImage, algorithm, rangeMeterWasUsed);
		}

		private ObjectVolumeData AggregateCalculationsData(IReadOnlyList<ObjectVolumeData> results)
		{
			try
			{
				var lengths = results.Where(r => r != null).Select(r => r.LengthMm).ToArray();
				var widths = results.Where(r => r != null).Select(r => r.WidthMm).ToArray();
				var heights = results.Where(r => r != null).Select(r => r.HeightMm).ToArray();

				var joinedLengths = string.Join(",", lengths);
				var joinedWidths = string.Join(",", widths);
				var joinedHeights = string.Join(",", heights);
				_logger.LogInfo($"Measured values: {{{joinedLengths}}}; {{{joinedWidths}}}; {{{joinedHeights}}}");

				var modeLength = lengths.GroupBy(x => x).OrderByDescending(g => g.Count()).First().Key;
				var modeWidth = widths.GroupBy(x => x).OrderByDescending(g => g.Count()).First().Key;
				var modeHeight = heights.GroupBy(x => x).OrderByDescending(g => g.Count()).First().Key;

				return new ObjectVolumeData(modeLength, modeWidth, modeHeight);
			}
			catch (Exception ex)
			{
				_logger.LogException("Failed to aggregate calculation results", ex);
				return null;
			}
		}
	}
}
