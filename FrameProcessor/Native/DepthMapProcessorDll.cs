using System.Runtime.InteropServices;

namespace FrameProcessor.Native
{
    internal static class DepthMapProcessorDll
    {
	    [DllImport(Constants.AnalyzerLibName, CallingConvention = CallingConvention.Cdecl)]
	    public static extern unsafe void CreateDepthMapProcessor(CameraIntrinsics colorIntrinsics, CameraIntrinsics depthIntrinsics);

	    [DllImport(Constants.AnalyzerLibName, CallingConvention = CallingConvention.Cdecl)]
	    public static extern unsafe void DestroyDepthMapProcessor();

		[DllImport(Constants.AnalyzerLibName, CallingConvention = CallingConvention.Cdecl)]
	    public static extern unsafe void SetAlgorithmSettings(short floorDepth, short cutOffDepth, 
			RelPoint* polygonPoints, int polygonPointCount, RelRect colorRoiRect);

	    [DllImport(Constants.AnalyzerLibName, CallingConvention = CallingConvention.Cdecl)]
	    public static extern unsafe void SetDebugPath(string path);

		[DllImport(Constants.AnalyzerLibName, CallingConvention = CallingConvention.Cdecl)]
	    public static extern unsafe VolumeCalculationResult* CalculateObjectVolume(VolumeCalculationData calculationData);

	    [DllImport(Constants.AnalyzerLibName, CallingConvention = CallingConvention.Cdecl)]
	    public static extern unsafe int SelectAlgorithm(DepthMap depthMap, ColorImage colorImage, long measuredDistance,
		    bool dm1Enabled, bool dm2Enabled, bool rgbEnabled);

		[DllImport(Constants.AnalyzerLibName, CallingConvention = CallingConvention.Cdecl)]
	    public static extern unsafe short CalculateFloorDepth(DepthMap depthMap);

		[DllImport(Constants.AnalyzerLibName, CallingConvention = CallingConvention.Cdecl)]
		public static extern unsafe void DisposeCalculationResult(VolumeCalculationResult* result);
	}
}