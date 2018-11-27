using System;
using FrameProcessor.Native;
using FrameProviders;
using Primitives;
using Primitives.Logging;
using DepthMap = Primitives.DepthMap;

namespace FrameProcessor
{
	public class DepthMapProcessor : IDisposable
	{
		private readonly ILogger _logger;
		private readonly IntPtr _nativeHandle;

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
		}

		public ObjectVolumeData CalculateVolume(DepthMap depthMap, bool needToSaveDebugData = false)
		{
			unsafe
			{
				fixed (short* depthData = depthMap.Data)
				{
					var nativeDepthMap = GetNativeDepthMapFromDepthMap(depthMap, depthData);

					var res = DepthMapProcessorDll.CalculateObjectVolume(_nativeHandle.ToPointer(), nativeDepthMap, needToSaveDebugData);
					return res == null ? null : new ObjectVolumeData(res->Length, res->Width, res->Height);
				}
			}
		}

		public ObjectVolumeData CalculateObjectVolumeAlt(DepthMap depthMap, ImageData colorFrame, bool needToSaveDebugData)
		{
			unsafe
			{
				fixed (short* depthData = depthMap.Data)
				fixed (byte* colorData = colorFrame.Data)
				{
					var nativeDepthMap = GetNativeDepthMapFromDepthMap(depthMap, depthData);

					var nativeColorImage = new ColorImage
					{
						Width = colorFrame.Width,
						Height = colorFrame.Height,
						Data = colorData,
						BytesPerPixel = colorFrame.BytesPerPixel
					};

					var res = DepthMapProcessorDll.CalculateObjectVolumeAlt(_nativeHandle.ToPointer(), nativeDepthMap, nativeColorImage, needToSaveDebugData);
					return res == null ? null : new ObjectVolumeData(res->Length, res->Width, res->Height);
				}
			}
		}

		public short CalculateFloorDepth(DepthMap depthMap)
		{
			unsafe
			{
				fixed (short* depthData = depthMap.Data)
				{
					var nativeDepthMap = GetNativeDepthMapFromDepthMap(depthMap, depthData);

					return DepthMapProcessorDll.CalculateFloorDepth(_nativeHandle.ToPointer(), nativeDepthMap);
				}
			}
		}

		public void SetProcessorSettings(ApplicationSettings settings)
		{
			var cutOffDepth = (short)(settings.FloorDepth - settings.MinObjectHeight);
			var colorRoiRect = CreateColorRoiRectFromSettings(settings);

			unsafe
			{
				DepthMapProcessorDll.SetAlgorithmSettings(_nativeHandle.ToPointer(), settings.FloorDepth, cutOffDepth, colorRoiRect);
				var terminatedPath = settings.PhotosDirectoryPath + "\0";
				DepthMapProcessorDll.SetDebugPath(_nativeHandle.ToPointer(), terminatedPath);
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

		private static unsafe Native.DepthMap GetNativeDepthMapFromDepthMap(DepthMap depthMap, short* depthData)
		{
			return new Native.DepthMap
			{
				Width = depthMap.Width,
				Height = depthMap.Height,
				Data = depthData
			};
		}

		private static RelRect CreateColorRoiRectFromSettings(ApplicationSettings settings)
		{
			float x1;
			float y1;
			float x2;
			float y2;

			if (settings != null && settings.UseDepthMask)
			{
				var rectanglePoints = settings.ColorMaskContour;
				x1 = (float)rectanglePoints[0].X;
				y1 = (float)rectanglePoints[0].Y;
				x2 = (float)rectanglePoints[2].X;
				y2 = (float)rectanglePoints[2].Y;
			}
			else
			{
				x1 = 0;
				y1 = 0;
				x2 = 1;
				y2 = 1;
			}

			return new RelRect
			{
				X = x1,
				Y = y1,
				Width = x2 - x1,
				Height = y2 - y1
			};
		}
	}
}