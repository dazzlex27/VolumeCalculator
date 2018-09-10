using System.Runtime.InteropServices;
using DepthMapProcessorGUI.Entities;

namespace DepthMapProcessorGUI.Utils
{
    internal static class DllWrapper
    {
		[DllImport("libDepthMapProcessor.dll", CallingConvention = CallingConvention.Cdecl)]
		public static extern int CreateDepthMapProcessor(float fovX, float fovY);

		[DllImport("libDepthMapProcessor.dll", CallingConvention = CallingConvention.Cdecl)]
		public static extern unsafe ObjDimDescription* CalculateObjectVolume(int mapWidth, int mapHeight, short* mapData);

	    [DllImport("libDepthMapProcessor.dll", CallingConvention = CallingConvention.Cdecl)]
	    public static extern unsafe short CalculateFloorDepth(int mapWidth, int mapHeight, short* mapData);

		[DllImport("libDepthMapProcessor.dll", CallingConvention = CallingConvention.Cdecl)]
	    public static extern void SetCalculatorSettings(short minDepth, short floorDepth, short cutOffDepth);

		[DllImport("libDepthMapProcessor.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern int DestroyDepthMapProcessor();
    }
}