using System;
using VolumeCheckerGUI.Entities;
using VolumeCheckerGUI.Utils;

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

		public ObjectVolumeData CalculateVolume(DepthMap depthMap)
		{
			unsafe
			{
				fixed (short* data = depthMap.Data)
				{
					var res = DllWrapper.CheckVolume(depthMap.Width, depthMap.Height, data);
					if (res == null)
						return null;

					var volume = res->Width * res->Height * res->Depth;
					return new ObjectVolumeData(res->Width, res->Height, res->Depth, volume);
				}
			}
		}

		public void SetSettings(short floorDepth, short cutOffDepth)
		{
			DllWrapper.SetCheckerSettings(Constants.MinDepth, floorDepth, cutOffDepth);
		}

		public void Dispose()
		{
			_logger.LogInfo("Disposing volume calculator...");
			DllWrapper.DestroyVolumeChecker();
		}
	}
}