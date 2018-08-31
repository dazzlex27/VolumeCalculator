using System;

namespace VolumeCheckerGUI.Logic
{
	internal class VolumeCalculator : IDisposable
	{
		private readonly Logger _logger;

		public VolumeCalculator(Logger logger)
		{
			_logger = logger;

			_logger.LogInfo("Creating volume calculator...");
		}

		public void Initialize(float fovX, float fovY)
		{
			_logger.LogInfo("Initializing volume calculator...");
			DllWrapper.CreateVolumeChecker(fovX, fovY);
		}

		public void Dispose()
		{
			_logger.LogInfo("Disposing volume calculator...");
			DllWrapper.DestroyVolumeChecker();
		}
	}
}