using System;
using System.Runtime.InteropServices;

namespace FrameProcessor.Native
{
	internal static class NativeMethods
	{
		[DllImport(Constants.AnalyzerLibName, CallingConvention = CallingConvention.Cdecl)]
		public static extern void CreateDepthMapProcessor(CameraIntrinsics colorIntrinsics, CameraIntrinsics depthIntrinsics);

		[DllImport(Constants.AnalyzerLibName, CallingConvention = CallingConvention.Cdecl)]
		public static extern void DestroyDepthMapProcessor();

		[DllImport(Constants.AnalyzerLibName, CallingConvention = CallingConvention.Cdecl)]
		public static extern unsafe void SetAlgorithmSettings(short floorDepth, short cutOffDepth,
			RelPoint* polygonPoints, int polygonPointCount, RelRect colorRoiRect);

		[DllImport(Constants.AnalyzerLibName, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Unicode)]
		public static extern void SetDebugDirectory(string path);

		[DllImport(Constants.AnalyzerLibName, CallingConvention = CallingConvention.Cdecl)]
		public static extern unsafe VolumeCalculationResult* CalculateObjectVolume(VolumeCalculationData data);

		[DllImport(Constants.AnalyzerLibName, CallingConvention = CallingConvention.Cdecl)]
		public static extern IntPtr SelectAlgorithm(NativeAlgorithmSelectionData data);

		[DllImport(Constants.AnalyzerLibName, CallingConvention = CallingConvention.Cdecl)]
		public static extern unsafe void DisposeAlgorithmSelectionResult(NativeAlgorithmSelectionResult* result);

		[DllImport(Constants.AnalyzerLibName, CallingConvention = CallingConvention.Cdecl)]
		public static extern short CalculateFloorDepth(DepthMap depthMap);

		[DllImport(Constants.AnalyzerLibName, CallingConvention = CallingConvention.Cdecl)]
		public static extern unsafe void DisposeCalculationResult(VolumeCalculationResult* result);
	}
}
