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
		private object _lock;

		public DepthMapProcessor(ILogger logger, ColorCameraParams colorCameraParams, DepthCameraParams depthCameraParams)
		{
			_lock = new object();

			_logger = logger;

			_logger.LogInfo("Creating depth map processor...");

			var colorIntrinsics = TypeConverter.ColorParamsToIntrinsics(colorCameraParams);
			var depthIntrinsics = TypeConverter.DepthParamsToIntrinsics(depthCameraParams);

			unsafe
			{
				DepthMapProcessorDll.CreateDepthMapProcessor(colorIntrinsics, depthIntrinsics);
			}
		}

		public ObjectVolumeData CalculateVolume(DepthMap depthMap, ImageData colorImage,
			long measuredDistance, int selectedAlgorithm, bool needToSaveDebugData, bool maskMode, int measurementNumber)
		{
			lock (_lock)
			{
				unsafe
				{
					fixed (short* depthData = depthMap.Data)
					fixed (byte* colorData = colorImage.Data)
					{
						var nativeDepthMap = GetNativeDepthMapFromDepthMap(depthMap, depthData);

						var nativeColorImage = new ColorImage
						{
							Width = colorImage.Width,
							Height = colorImage.Height,
							Data = colorData,
							BytesPerPixel = colorImage.BytesPerPixel
						};

						var volumeCalculationData = new VolumeCalculationData()
						{
							DepthMap = &nativeDepthMap,
							Image = &nativeColorImage,
							CalculationNumber = measurementNumber,
							MaskMode = maskMode,
							SaveDebugData = needToSaveDebugData,
							SelectedAlgorithm = selectedAlgorithm,
							RangeMeterDistance = measuredDistance
						};

						var nativeResult = DepthMapProcessorDll.CalculateObjectVolume(volumeCalculationData);

						_logger.LogInfo($"native: {nativeResult->LengthMm} {nativeResult->WidthMm} {nativeResult->HeightMm}");

						var result = nativeResult == null ?
							null : new ObjectVolumeData(nativeResult->LengthMm, nativeResult->WidthMm, nativeResult->HeightMm, nativeResult->VolumeCmCb);
						DepthMapProcessorDll.DisposeCalculationResult(nativeResult);

						return result;
					}
				}
			}
		}

		public short CalculateFloorDepth(DepthMap depthMap)
		{
			lock (_lock)
			{
				unsafe
				{
					fixed (short* depthData = depthMap.Data)
					{
						var nativeDepthMap = GetNativeDepthMapFromDepthMap(depthMap, depthData);

						return DepthMapProcessorDll.CalculateFloorDepth(nativeDepthMap);
					}
				}
			}
		}

		public AlgorithmSelectionResult SelectAlgorithm(DepthMap depthMap, ImageData colorFrame, long measuredDistance,
			bool dm1Enabled, bool dm2Enabled, bool rgbEnabled)
		{
			lock (_lock)
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

						return (AlgorithmSelectionResult)DepthMapProcessorDll.SelectAlgorithm(
							nativeDepthMap, nativeColorImage, measuredDistance, dm1Enabled, dm2Enabled, rgbEnabled);
					}
				}
			}
		}

		public void SetProcessorSettings(ApplicationSettings settings)
		{
			lock (_lock)
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
						DepthMapProcessorDll.SetAlgorithmSettings(settings.AlgorithmSettings.FloorDepth, cutOffDepth,
							points, relPoints.Length, colorRoiRect);
					}

					var terminatedPath = settings.IoSettings.PhotosDirectoryPath + "\0";
					DepthMapProcessorDll.SetDebugPath(terminatedPath);
				}
			}
		}

		public void Dispose()
		{
			_logger.LogInfo("Disposing depth map processor...");
			unsafe
			{
				DepthMapProcessorDll.DestroyDepthMapProcessor();
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