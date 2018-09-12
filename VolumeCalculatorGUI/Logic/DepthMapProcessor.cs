using System;
using Common;
using FrameSources;
using VolumeCalculatorGUI.Entities;
namespace VolumeCalculatorGUI.Logic
{
	internal class DepthMapProcessor : IDisposable
	{
		private readonly ILogger _logger;
		private readonly IntPtr _nativeHandle;

		public DepthMapProcessor(ILogger logger, DeviceParams deviceParams)
		{
			_logger = logger;

			_logger.LogInfo("Creating depth map processor...");

			unsafe
			{
				var ptr = DepthMapProcessorDll.CreateDepthMapProcessor(deviceParams.FocalLengthX,
					deviceParams.FocalLengthY,
					deviceParams.PrincipalX, deviceParams.PrincipalY, deviceParams.MinDepth, deviceParams.MaxDepth);

				_nativeHandle = new IntPtr(ptr);
			}
		}

		public ObjectVolumeData CalculateVolume(DepthMap depthMap)
		{
			unsafe
			{
				fixed (short* data = depthMap.Data)
				{
					var res = DepthMapProcessorDll.CalculateObjectVolume(_nativeHandle.ToPointer(), depthMap.Width, depthMap.Height, data);
					return res == null ? null : new ObjectVolumeData(res->Width, res->Height, res->Depth);
				}
			}
		}

		public short CalculateFloorDepth(DepthMap depthMap)
		{
			unsafe
			{
				fixed (short* data = depthMap.Data)
				{
					return DepthMapProcessorDll.CalculateFloorDepth(_nativeHandle.ToPointer(), depthMap.Width, depthMap.Height, data);
				}
			}
		}

		public void SetCalculatorSettings(short floorDepth, short cutOffDepth)
		{
			unsafe
			{
				DepthMapProcessorDll.SetCalculatorSettings(_nativeHandle.ToPointer(), floorDepth, cutOffDepth);
			}
		}

		public void Dispose()
		{
			_logger.LogInfo("Disposing depth map processor...");
			unsafe
			{
				DepthMapProcessorDll.DestroyDepthMapProcessor(_nativeHandle.ToPointer());
			}
		}
	}
}