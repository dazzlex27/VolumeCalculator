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
			AlgorithmSelectionStatus selectedAlgorithm)
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

		public AlgorithmSelectionResult SelectAlgorithm(AlgorithmSelectionData data)
		{
			try
			{


				if (data == null)
					return new AlgorithmSelectionResult(AlgorithmSelectionStatus.DataIsInvalid, false);

				lock (_lock)
				{
					var colorFrame = data.Image;
					var depthMap = data.DepthMap;

					var dataIsInvalid = data.DepthMap?.Data == null || colorFrame?.Data == null;
					if (dataIsInvalid)
						return new AlgorithmSelectionResult(AlgorithmSelectionStatus.DataIsInvalid, false);

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

							var debugFilename = data.DebugFileName;

							var algorithmSelectionData = new NativeAlgorithmSelectionData
							{
								DepthMap = &nativeDepthMap,
								ColorImage = &nativeColorImage,
								CalculatedDistance = data.CalculatedDistance,
								Dm1Enabled = data.Dm1Enabled,
								Dm2Enabled = data.Dm2Enabled,
								RgbEnabled = data.RgbEnabled,
								DebugFileName = debugFilename.Length > 127
									? debugFilename.Substring(0, 127)
									: debugFilename
							};

							var nativeResult = (NativeAlgorithmSelectionResult*)DepthMapProcessorDll.SelectAlgorithm(algorithmSelectionData);

							var rangeMeterWasUsed = nativeResult->RangeMeterWasUsed > 0;
							var algorithmSelectionResult = new AlgorithmSelectionResult(nativeResult->Status, rangeMeterWasUsed); 
							
							DepthMapProcessorDll.DisposeAlgorithmSelectionResult(nativeResult);

							return algorithmSelectionResult;
						}
					}
				}
			}
			catch (Exception ex)
			{
				_logger.LogException("Failed to run algorithm selection", ex);
			}
			
			return new AlgorithmSelectionResult(AlgorithmSelectionStatus.Undefined, false);
		}

		public void SetProcessorSettings(ApplicationSettings settings)
		{
			lock (_lock)
			{
				SetWorkAreaSettings(settings.AlgorithmSettings.WorkArea);

				var terminatedPath = settings.GeneralSettings.PhotosDirectoryPath + "\0";
				DepthMapProcessorDll.SetDebugPath(terminatedPath, false);
			}
		}

		public void SetWorkAreaSettings(WorkAreaSettings workAreaSettings)
		{
			var cutOffDepth = (short)(workAreaSettings.FloorDepth - workAreaSettings.MinObjectHeight);
			var colorRoiRect = CreateColorRoiRectFromSettings(workAreaSettings);

			unsafe
			{
				var relPoints = new RelPoint[workAreaSettings.DepthMaskContour.Count];
				for (var i = 0; i < relPoints.Length; i++)
				{
					relPoints[i].X = (float) workAreaSettings.DepthMaskContour[i].X;
					relPoints[i].Y = (float) workAreaSettings.DepthMaskContour[i].Y;
				}

				fixed (RelPoint* points = relPoints)
				{
					DepthMapProcessorDll.SetAlgorithmSettings(workAreaSettings.FloorDepth, cutOffDepth,
						points, relPoints.Length, colorRoiRect);
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

		private static RelRect CreateColorRoiRectFromSettings(WorkAreaSettings workAreaSettings)
		{
			float x1;
			float y1;
			float x2;
			float y2;

			if (workAreaSettings != null && workAreaSettings.UseDepthMask)
			{
				var rectanglePoints = workAreaSettings.ColorMaskContour;
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