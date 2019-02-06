using System;
using FrameProcessor.Native;
using FrameProviders;
using Primitives;
using Primitives.Logging;
using Primitives.Settings;
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

		public ObjectVolumeData CalculateVolumeDepth(DepthMap depthMap, bool applyPerspective, bool needToSaveDebugData = false,
			bool maskMode = false)
		{
			unsafe
			{
				fixed (short* depthData = depthMap.Data)
				{
					var nativeDepthMap = GetNativeDepthMapFromDepthMap(depthMap, depthData);

					var res = DepthMapProcessorDll.CalculateObjectVolume(_nativeHandle.ToPointer(), nativeDepthMap, applyPerspective, 
						needToSaveDebugData, maskMode);
					return res == null ? null : new ObjectVolumeData(res->Length, res->Width, res->Height);
				}
			}
		}

		public ObjectVolumeData CalculateObjectVolumeRgb(DepthMap depthMap, ImageData colorFrame, bool applyPerspective, 
			bool needToSaveDebugData = false, bool maskMode = false)
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

					var res = DepthMapProcessorDll.CalculateObjectVolumeAlt(_nativeHandle.ToPointer(), nativeDepthMap, 
						nativeColorImage, applyPerspective, needToSaveDebugData, maskMode);
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

		public AlgorithmSelectionResult SelectAlgorithm(DepthMap depthMap, ImageData colorFrame, 
			bool dm1Enabled, bool dm2Enabled, bool rgbEnabled)
		{
			var dataIsInvalid = depthMap?.Data == null || colorFrame?.Data == null;
			if (dataIsInvalid)
				return AlgorithmSelectionResult.DataIsInvalid;

			var atLeastOneModeIsAvailable = dm1Enabled || dm2Enabled || rgbEnabled;
			if (!atLeastOneModeIsAvailable)
				return AlgorithmSelectionResult.NoModesAreAvailable;

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

					return (AlgorithmSelectionResult) DepthMapProcessorDll.SelectAlgorithm(_nativeHandle.ToPointer(),
						nativeDepthMap, nativeColorImage, dm1Enabled, dm2Enabled, rgbEnabled);
				}
			}
		}

		public void SetProcessorSettings(ApplicationSettings settings)
		{
			var cutOffDepth = (short)(settings.AlgorithmSettings.FloorDepth - settings.AlgorithmSettings.MinObjectHeight);
			var colorRoiRect = CreateColorRoiRectFromSettings(settings);

			unsafe
			{
				var relPoints = new RelPoint[settings.AlgorithmSettings.DepthMaskContour.Count];
				for (var i = 0; i < relPoints.Length; i++)
				{
					relPoints[i].X = (float)settings.AlgorithmSettings.DepthMaskContour[i].X;
					relPoints[i].Y = (float)settings.AlgorithmSettings.DepthMaskContour[i].Y;
				}

				fixed (RelPoint* points = relPoints)
				{
					DepthMapProcessorDll.SetAlgorithmSettings(_nativeHandle.ToPointer(), settings.AlgorithmSettings.FloorDepth, cutOffDepth, 
						points, relPoints.Length, colorRoiRect);
				}

				var terminatedPath = settings.IoSettings.PhotosDirectoryPath + "\0";
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

			if (settings != null && settings.AlgorithmSettings.UseDepthMask)
			{
				var rectanglePoints = settings.AlgorithmSettings.ColorMaskContour;
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