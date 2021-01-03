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

		public FakeRangeMeter(ILogger logger)
		{
			_logger = logger;
			_logger.LogInfo("Creating a fake range meter...");

			_lastDistance = 0;
		}

		public void Dispose()
		{
			_logger.LogInfo("Disposing fake range meter...");
		}

		public long GetReading()
		{
			Thread.Sleep(500);

			var lastDistanceMm = _lastDistance / 10 - DefaultSubtractionValueMm;
			_lastDistance = 0;
			return lastDistanceMm;
		}

		public void ToggleLaser(bool enable)
		{
		}
	}
}