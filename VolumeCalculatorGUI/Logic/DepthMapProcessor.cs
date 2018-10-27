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
		private ApplicationSettings _settings;

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
			float x1;
			float y1;
			float x2;
			float y2;

			if (_settings != null && _settings.UseDepthMask)
			{
				var rectanglePoints = _settings.ColorAreaContour;
				x1 = (float) rectanglePoints[0].X;
				y1 = (float) rectanglePoints[0].Y;
				x2 = (float) rectanglePoints[2].X;
				y2 = (float) rectanglePoints[2].Y;
			}
			else
			{
				x1 = 0;
				y1 = 0;
				x2 = 1;
				y2 = 1;
			}

			unsafe
			{
				fixed (byte* colorData = colorFrame.Data)
				fixed (short* depthData = depthFrame.Data)
				{
					var relRect = new RelRect
					{
						X = x1,
						Y = y1,
						Width = x2 - x1,
						Height = y2 - y1
					};

					var res = DepthMapProcessorDll.CalculateObjectVolumeAlt(_nativeHandle.ToPointer(), colorFrame.Width,
						colorFrame.Height, colorData, colorFrame.BytesPerPixel, relRect, depthFrame.Width,
						depthFrame.Height, depthData);
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

		public void SetCalculatorSettings(ApplicationSettings settings)
		{
			_settings = settings;

			unsafe
			{
				var cutOffDepth = (short) (_settings.DistanceToFloor - _settings.MinObjHeight);
				DepthMapProcessorDll.SetCalculatorSettings(_nativeHandle.ToPointer(), _settings.DistanceToFloor, cutOffDepth);
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