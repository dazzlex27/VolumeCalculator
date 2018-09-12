using System.Runtime.InteropServices;
using VolumeCalculatorGUI.Entities;

namespace VolumeCalculatorGUI.Logic
{
    internal static class DepthMapProcessorDll
    {
	    [DllImport("libDepthMapProcessor.dll", CallingConvention = CallingConvention.Cdecl)]
	    public static extern unsafe void* CreateDepthMapProcessor(float focalLengthX, float focalLengthY, float principalX,
		    float principalY, short minDepth, short maxDepth);

		[DllImport("libDepthMapProcessor.dll", CallingConvention = CallingConvention.Cdecl)]
		public static extern unsafe ObjDimDescription* CalculateObjectVolume(void* handle, int mapWidth, int mapHeight, short* mapData);

	    [DllImport("libDepthMapProcessor.dll", CallingConvention = CallingConvention.Cdecl)]
	    public static extern unsafe short CalculateFloorDepth(void* handle, int mapWidth, int mapHeight, short* mapData);

		[DllImport("libDepthMapProcessor.dll", CallingConvention = CallingConvention.Cdecl)]
	    public static extern unsafe void SetCalculatorSettings(void* handle, short floorDepth, short cutOffDepth);

		[DllImport("libDepthMapProcessor.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern unsafe void DestroyDepthMapProcessor(void* handle);
    }
}