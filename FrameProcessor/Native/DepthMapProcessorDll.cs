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
	    public static extern unsafe bool AreThereObjectsInZone(void* handle, DepthMap depthMap);

		[DllImport(Constants.AnalyzerLibName, CallingConvention = CallingConvention.Cdecl)]
	    public static extern unsafe ObjDimDescription* CalculateObjectVolume(void* handle, DepthMap depthMap, bool saveDebugData);

	    [DllImport(Constants.AnalyzerLibName, CallingConvention = CallingConvention.Cdecl)]
	    public static extern unsafe ObjDimDescription* CalculateObjectVolumeAlt(void* handle, DepthMap depthMap, ColorImage image, bool saveDebugData);

		[DllImport(Constants.AnalyzerLibName, CallingConvention = CallingConvention.Cdecl)]
	    public static extern unsafe short CalculateFloorDepth(void* handle, DepthMap depthMap);
    }
}