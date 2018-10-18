using System;
using Common;
using FrameProviders;
using VolumeCalculatorGUI.Entities;
using VolumeCalculatorGUI.Utils;

namespace VolumeCalculatorGUI.Logic
{
	internal class DepthMapProcessor : IDisposable
	{
		private readonly ILogger _logger;
		private readonly IntPtr _nativeHandle;

		public bool IsActive { get; private set; }

		public DepthMapProcessor(ILogger logger, ColorCameraParams colorCameraParams, DepthCameraParams depthCameraParams)
		{
			_logger = logger;

			_logger.LogInfo("Creating depth map processor...");

			var colorIntrinsics = TypeConverter.ColorParamsToIntrinsics(colorCameraParams);
			var depthIntrinsics = TypeConverter.DepthParamsToIntrinsics(depthCameraParams);

			unsafe
			{
				var ptr = DepthMapProcessorDll.CreateDepthMapProcessor(colorIntrinsics, depthIntrinsics);
				_nativeHandle = new IntPtr(ptr);
			}

			IsActive = true;
		}

		public ObjectVolumeData CalculateVolume(DepthMap depthMap)
		{
			unsafe
			{
				fixed (short* data = depthMap.Data)
				{
					var res = DepthMapProcessorDll.CalculateObjectVolume(_nativeHandle.ToPointer(), depthMap.Width, 
						depthMap.Height, data);
					return res == null ? null : new ObjectVolumeData(res->Length, res->Width, res->Height);
				}
			}
		}

		public ObjectVolumeData CalculateObjectVolumeAlt(ImageData colorFrame, DepthMap depthFrame)
		{
			unsafe
			{
				fixed (byte* colorData = colorFrame.Data)
				fixed (short* depthData = depthFrame.Data)
				{
					var res = DepthMapProcessorDll.CalculateObjectVolumeAlt(_nativeHandle.ToPointer(), colorFrame.Width,
						colorFrame.Height, colorData, colorFrame.BytesPerPixel, depthFrame.Width, depthFrame.Height, depthData);
					return res == null ? null : new ObjectVolumeData(res->Length, res->Width, res->Height);
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
			IsActive = false;

			_logger.LogInfo("Disposing depth map processor...");
			unsafe
			{
				DepthMapProcessorDll.DestroyDepthMapProcessor(_nativeHandle.ToPointer());
			}
		}
	}
}