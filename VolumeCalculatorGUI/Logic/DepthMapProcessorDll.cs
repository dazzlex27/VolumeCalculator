using System.Runtime.InteropServices;
using VolumeCalculatorGUI.Entities;

namespace VolumeCalculatorGUI.Logic
{
    internal static class DepthMapProcessorDll
    {
	    [DllImport("libDepthMapProcessor.dll", CallingConvention = CallingConvention.Cdecl)]
	    public static extern unsafe void* CreateDepthMapProcessor(ColorCameraIntrinsics colorIntrinsics, DepthCameraIntrinsics depthIntrinsics);

		[DllImport("libDepthMapProcessor.dll", CallingConvention = CallingConvention.Cdecl)]
		public static extern unsafe ObjDimDescription* CalculateObjectVolume(void* handle, int mapWidth, int mapHeight, short* mapData);

	    [DllImport("libDepthMapProcessor.dll", CallingConvention = CallingConvention.Cdecl)]
	    public static extern unsafe ObjDimDescription* CalculateObjectVolumeAlt(void* handle, int imageWidth, int imageHeight, 
		    byte* imageData, int bytesPerPixel, RelRect roiRect, int mapWidth, int mapHeight, short* depthData);

		[DllImport("libDepthMapProcessor.dll", CallingConvention = CallingConvention.Cdecl)]
	    public static extern unsafe short CalculateFloorDepth(void* handle, int mapWidth, int mapHeight, short* mapData);

		[DllImport("libDepthMapProcessor.dll", CallingConvention = CallingConvention.Cdecl)]
	    public static extern unsafe void SetCalculatorSettings(void* handle, short floorDepth, short cutOffDepth);

		[DllImport("libDepthMapProcessor.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern unsafe void DestroyDepthMapProcessor(void* handle);
    }
}