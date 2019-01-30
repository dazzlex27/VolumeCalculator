using System.Runtime.InteropServices;

namespace FrameProcessor.Native
{
    internal static class DepthMapProcessorDll
    {
	    [DllImport(Constants.AnalyzerLibName, CallingConvention = CallingConvention.Cdecl)]
	    public static extern unsafe void* CreateDepthMapProcessor(ColorCameraIntrinsics colorIntrinsics, 
		    DepthCameraIntrinsics depthIntrinsics);

	    [DllImport(Constants.AnalyzerLibName, CallingConvention = CallingConvention.Cdecl)]
	    public static extern unsafe void DestroyDepthMapProcessor(void* handle);

		[DllImport(Constants.AnalyzerLibName, CallingConvention = CallingConvention.Cdecl)]
	    public static extern unsafe void SetAlgorithmSettings(void* handle, short floorDepth, short cutOffDepth, 
			RelPoint* polygonPoints, int polygonPointCount, RelRect colorRoiRect);

	    [DllImport(Constants.AnalyzerLibName, CallingConvention = CallingConvention.Cdecl)]
	    public static extern unsafe void SetDebugPath(void* handle, string path);

		[DllImport(Constants.AnalyzerLibName, CallingConvention = CallingConvention.Cdecl)]
	    public static extern unsafe ObjDimDescription* CalculateObjectVolume(void* handle, DepthMap depthMap, bool applyPerspective, 
			bool saveDebugData, bool maskMode);

	    [DllImport(Constants.AnalyzerLibName, CallingConvention = CallingConvention.Cdecl)]
	    public static extern unsafe ObjDimDescription* CalculateObjectVolumeAlt(void* handle, DepthMap depthMap, ColorImage image, 
		    bool applyPerspective, bool saveDebugData, bool maskMode);

	    [DllImport(Constants.AnalyzerLibName, CallingConvention = CallingConvention.Cdecl)]
	    public static extern unsafe int SelectAlgorithm(void* handle, DepthMap depthMap, ColorImage colorImage,
		    bool dm1Enabled, bool dm2Enabled, bool rgbEnabled);

		[DllImport(Constants.AnalyzerLibName, CallingConvention = CallingConvention.Cdecl)]
	    public static extern unsafe short CalculateFloorDepth(void* handle, DepthMap depthMap);
    }
}