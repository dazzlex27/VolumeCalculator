using System;
using System.Runtime.InteropServices;

namespace FrameProcessor.Native
{
	internal static class NativeMethods
	{
		[DllImport(Constants.AnalyzerLibName, CallingConvention = CallingConvention.Cdecl)]
		public static extern IntPtr CreateDepthMapProcessor(CameraIntrinsics colorIntrinsics, CameraIntrinsics depthIntrinsics);

		[DllImport(Constants.AnalyzerLibName, CallingConvention = CallingConvention.Cdecl)]
		public static extern void DestroyDepthMapProcessor(IntPtr processor);

		[DllImport(Constants.AnalyzerLibName, CallingConvention = CallingConvention.Cdecl)]
		public static extern unsafe void SetAlgorithmSettings(IntPtr processor, short floorDepth, short cutOffDepth,
			RelPoint* polygonPoints, int polygonPointCount, RelRect colorRoiRect);

		[DllImport(Constants.AnalyzerLibName, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Unicode)]
		public static extern void SetDebugDirectory(IntPtr processor, string path);

		[DllImport(Constants.AnalyzerLibName, CallingConvention = CallingConvention.Cdecl)]
		public static extern unsafe VolumeCalculationResult* CalculateObjectVolume(IntPtr processor, VolumeCalculationData data);

		[DllImport(Constants.AnalyzerLibName, CallingConvention = CallingConvention.Cdecl)]
		public static extern IntPtr SelectAlgorithm(IntPtr processor, NativeAlgorithmSelectionData data);

		[DllImport(Constants.AnalyzerLibName, CallingConvention = CallingConvention.Cdecl)]
		public static extern unsafe void DisposeAlgorithmSelectionResult(NativeAlgorithmSelectionResult* result);

		[DllImport(Constants.AnalyzerLibName, CallingConvention = CallingConvention.Cdecl)]
		public static extern short CalculateFloorDepth(IntPtr processor, DepthMap depthMap);

		[DllImport(Constants.AnalyzerLibName, CallingConvention = CallingConvention.Cdecl)]
		public static extern unsafe void DisposeCalculationResult(VolumeCalculationResult* result);
	}
}
