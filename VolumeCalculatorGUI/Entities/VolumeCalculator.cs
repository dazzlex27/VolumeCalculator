﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Timers;
using Common;
using VolumeCalculatorGUI.Entities;

namespace VolumeCalculatorGUI.Logic
{
	internal class VolumeCalculator
	{
		public event Action CalculationCancelled;
		public event Action<ObjectVolumeData> CalculationFinished;

		private readonly ILogger _logger;
		private readonly DepthMapProcessor _processor;
		private readonly bool _useColorVersion;
		private readonly List<ObjectVolumeData> _results;

		private int _samplesLeft;

		private readonly Timer _timer;

		public VolumeCalculator(ILogger logger, DepthMapProcessor processor, bool useColorVersion, int sampleSize)
		{
			_logger = logger;
			_processor = processor;
			_samplesLeft = sampleSize;
			_useColorVersion = useColorVersion;

			_results = new List<ObjectVolumeData>();

			IsActive = true;

			_timer = new Timer(3000);
			_timer.Elapsed += Timer_Elapsed;
			_timer.Start();
		}

		public bool IsActive { get; private set; }

		public void AdvanceCalculation(ImageData image, DepthMap depthMap)
		{
			_timer.Stop();

			var currentResult = _useColorVersion
				? _processor.CalculateObjectVolumeAlt(image, depthMap)
				: _processor.CalculateVolume(depthMap);
			_results.Add(currentResult);
			_samplesLeft--;

			if (_samplesLeft > 0)
			{
				_timer.Start();
				return;
			}

			IsActive = false;
			var totalResult = AggregateCalculationsData();
			CalculationFinished?.Invoke(totalResult);
		}

		private ObjectVolumeData AggregateCalculationsData()
		{
			try
			{
				var lengths = _results.Select(r => r.Length).ToArray();
				var widths = _results.Select(r => r.Width).ToArray();
				var heights = _results.Select(r => r.Height).ToArray();

				var modeLength = lengths.GroupBy(x => x).OrderByDescending(g => g.Count()).First().Key;
				var modeWidth = widths.GroupBy(x => x).OrderByDescending(g => g.Count()).First().Key;
				var modeHeight = heights.GroupBy(x => x).OrderByDescending(g => g.Count()).First().Key;

				return new ObjectVolumeData(modeLength, modeWidth, modeHeight);
			}
			catch (Exception ex)
			{
				_logger.LogException("Failed to aggregate calculation results", ex);
				return new ObjectVolumeData(0, 0, 0);
			}
		}

		private void Timer_Elapsed(object sender, ElapsedEventArgs e)
		{
			CalculationCancelled?.Invoke();
		}
	}
}