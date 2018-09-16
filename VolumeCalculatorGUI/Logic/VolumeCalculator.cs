using System;
using System.Collections.Generic;
using System.Linq;
using Common;
using VolumeCalculatorGUI.Entities;

namespace VolumeCalculatorGUI.Logic
{
	internal class VolumeCalculator
	{
		public event Action<ObjectVolumeData> CalculationFinished;

		private readonly List<ObjectVolumeData> _results;
		private readonly DepthMapProcessor _processor;

		private int _samplesLeft;

		public VolumeCalculator(DepthMapProcessor processor, int sampleSize)
		{
			_results = new List<ObjectVolumeData>();

			_processor = processor;
			_samplesLeft = sampleSize;

			IsActive = true;
		}

		public bool IsActive { get; private set; }

		public void AdvanceCalculation(DepthMap depthMap)
		{
			var currentResult = _processor.CalculateVolume(depthMap);
			_results.Add(currentResult);
			_samplesLeft--;

			if (_samplesLeft > 0)
				return;

			IsActive = false;
			var totalResult = AggregateCalculationsData();
			CalculationFinished?.Invoke(totalResult);
		}

		private ObjectVolumeData AggregateCalculationsData()
		{
			var lengths = _results.Select(r => r.Length).ToArray();
			var widths = _results.Select(r => r.Width).ToArray();
			var heights = _results.Select(r => r.Height).ToArray();

			var modeLength = lengths.GroupBy(x => x).OrderByDescending(g => g.Count()).First().Key;
			var modeWidth = widths.GroupBy(x => x).OrderByDescending(g => g.Count()).First().Key;
			var modeHeight = heights.GroupBy(x => x).OrderByDescending(g => g.Count()).First().Key;

			return new ObjectVolumeData(modeLength, modeWidth, modeHeight);
		}
	}
}