using System;
using DepthMapProcessorGUI.Entities;
using DepthMapProcessorGUI.Utils;

namespace DepthMapProcessorGUI.Logic
{
	internal class DepthMapProcessor : IDisposable
	{
		private readonly Logger _logger;

		public DepthMapProcessor(Logger logger)
		{
			_logger = logger;

			_logger.LogInfo("Creating depth map processor...");
		}

		public void Initialize(float fovX, float fovY)
		{
			_logger.LogInfo("Initializing depth map processor...");
			DllWrapper.CreateDepthMapProcessor(fovX, fovY);
		}

		public ObjectVolumeData CalculateVolume(DepthMap depthMap)
		{
			unsafe
			{
				fixed (short* data = depthMap.Data)
				{
					var res = DllWrapper.CalculateObjectVolume(depthMap.Width, depthMap.Height, data);
					if (res == null)
						return null;

					var volume = res->Width * res->Height * res->Depth;
					return new ObjectVolumeData(res->Width, res->Height, res->Depth, volume);
				}
			}
		}

		public short CalculateFloorDepth(DepthMap depthMap)
		{
			unsafe
			{
				fixed (short* data = depthMap.Data)
				{
					return DllWrapper.CalculateFloorDepth(depthMap.Width, depthMap.Height, data);
				}
			}
		}

		public void SetSettings(short floorDepth, short cutOffDepth)
		{
			DllWrapper.SetCalculatorSettings(Constants.MinDepth, floorDepth, cutOffDepth);
		}

		public void Dispose()
		{
			_logger.LogInfo("Disposing depth map processor...");
			DllWrapper.DestroyDepthMapProcessor();
		}
	}
}