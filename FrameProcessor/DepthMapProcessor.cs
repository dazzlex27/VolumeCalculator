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
		private readonly object _lock;

		public DepthMapProcessor(ILogger logger, ColorCameraParams colorCameraParams, DepthCameraParams depthCameraParams)
		{
			_lock = new object();

			_logger = logger;

			_logger.LogInfo("Creating depth map processor...");

			var colorIntrinsics = TypeConverter.ColorParamsToIntrinsics(colorCameraParams);
			var depthIntrinsics = TypeConverter.DepthParamsToIntrinsics(depthCameraParams);

			DepthMapProcessorDll.CreateDepthMapProcessor(colorIntrinsics, depthIntrinsics);
		}

		public ObjectVolumeData CalculateVolume(DepthMap depthMap, ImageData colorImage, short calculatedDistance, 
			AlgorithmSelectionResult selectedAlgorithm)
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

						var volumeCalculationData = new VolumeCalculationData
						{
							DepthMap = &nativeDepthMap,
							ColorImage = &nativeColorImage,
							SelectedAlgorithm = selectedAlgorithm,
							CalculatedDistance = calculatedDistance
						};

						var nativeResult = DepthMapProcessorDll.CalculateObjectVolume(volumeCalculationData);

						var result = nativeResult == null ?
							null : new ObjectVolumeData(nativeResult->LengthMm, nativeResult->WidthMm, nativeResult->HeightMm);
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

		public AlgorithmSelectionResult SelectAlgorithm(DepthMap depthMap, ImageData colorFrame, short calculatedDistance,
			bool dm1Enabled, bool dm2Enabled, bool rgbEnabled, string debugFileName)
		{
			lock (_lock)
			{
				var dataIsInvalid = depthMap?.Data == null || colorFrame?.Data == null;
				if (dataIsInvalid)
					return AlgorithmSelectionResult.DataIsInvalid;

				var atLeastOneModeIsAvailable = dm1Enabled || dm2Enabled || rgbEnabled;
				if (!atLeastOneModeIsAvailable)
					return AlgorithmSelectionResult.NoAlgorithmsAllowed;

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

						var algorithmSelectionData = new AlgorithmSelectionData()
						{
							DepthMap = &nativeDepthMap,
							ColorImage = &nativeColorImage,
							CalculatedDistance = calculatedDistance,
							Dm1Enabled = dm1Enabled,
							Dm2Enabled = dm2Enabled,
							RgbEnabled = rgbEnabled,
							DebugFileName = debugFileName.Length > 128 ? debugFileName.Substring(0, 128) : debugFileName
						};

						return DepthMapProcessorDll.SelectAlgorithm(algorithmSelectionData);
					}
				}
			}
		}

		public void SetProcessorSettings(ApplicationSettings settings)
		{
			lock (_lock)
			{
				var cutOffDepth = (short)(settings.AlgorithmSettings.WorkArea.FloorDepth - settings.AlgorithmSettings.WorkArea.MinObjectHeight);
				var colorRoiRect = CreateColorRoiRectFromSettings(settings);

				unsafe
				{
					var relPoints = new RelPoint[settings.AlgorithmSettings.WorkArea.DepthMaskContour.Count];
					for (var i = 0; i < relPoints.Length; i++)
					{
						relPoints[i].X = (float)settings.AlgorithmSettings.WorkArea.DepthMaskContour[i].X;
						relPoints[i].Y = (float)settings.AlgorithmSettings.WorkArea.DepthMaskContour[i].Y;
					}

					fixed (RelPoint* points = relPoints)
					{
						DepthMapProcessorDll.SetAlgorithmSettings(settings.AlgorithmSettings.WorkArea.FloorDepth, cutOffDepth,
							points, relPoints.Length, colorRoiRect);
					}

					var terminatedPath = settings.IoSettings.PhotosDirectoryPath + "\0";
					DepthMapProcessorDll.SetDebugPath(terminatedPath, false);
				}
			}
		}

		public void Dispose()
		{
			_logger.LogInfo("Disposing depth map processor...");
			DepthMapProcessorDll.DestroyDepthMapProcessor();
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

			if (settings != null && settings.AlgorithmSettings.WorkArea.UseDepthMask)
			{
				var rectanglePoints = settings.AlgorithmSettings.WorkArea.ColorMaskContour;
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