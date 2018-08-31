using System;
using VolumeCheckerGUI.Structures;

namespace VolumeCheckerGUI.Logic
{
	internal class FrameFeeder : IDisposable
	{
		private readonly Logger _logger;

		public event Action<DepthMap> DepthFrameReady;

		public FrameFeeder(Logger logger)
		{
			_logger = logger;

			_logger.LogInfo("Creating frame feeder...");
		}

		public void Start()
		{
			_logger.LogInfo("Starting frame feeder...");
			DllWrapper.CreateFrameFeeder();
		}

		public void Dispose()
		{
			_logger.LogInfo("Disposing frame feeder...");
			DllWrapper.DestroyFrameFeeder();
		}
	}
}