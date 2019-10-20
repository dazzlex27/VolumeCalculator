using System;
using System.Threading;
using Primitives.Logging;

namespace DeviceIntegration.RangeMeters
{
	internal class FakeRangeMeter : IRangeMeter
	{
		private const int DefaultSubtractionValueMm = 140;

		private readonly ILogger _logger;

		private long _lastDistance;
		private int _subtractionValueMm;

		public FakeRangeMeter(ILogger logger)
		{
			_logger = logger;
			_logger.LogInfo("Creating a fake range meter...");

			_lastDistance = 0;
			_subtractionValueMm = 0;
		}

		public void Dispose()
		{
			_logger.LogInfo("Disposing fake range meter...");
		}

		public void SetSubtractionValueMm(int value)
		{
			_subtractionValueMm = value;
		}

		public long GetReading()
		{
			Thread.Sleep(500);

			var totalSubtractionValue = DefaultSubtractionValueMm + _subtractionValueMm;
			var lastDistanceMm = _lastDistance / 10 - totalSubtractionValue;
			_lastDistance = 0;
			return lastDistanceMm;
		}

		public void ToggleLaser(bool enable)
		{
		}
	}
}