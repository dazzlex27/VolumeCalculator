using System.Runtime.InteropServices;
using DepthMapProcessorGUI.Entities;

namespace DepthMapProcessorGUI.Utils
{
    internal static class DllWrapper
    {
	    [UnmanagedFunctionPointer(CallingConvention.StdCall)]
	    public unsafe delegate void ColorFrameCallback(ColorFrame* frame);

	    [UnmanagedFunctionPointer(CallingConvention.StdCall)]
	    public unsafe delegate void DepthFrameCallback(DepthFrame* frame);

		[DllImport("libFrameFeeder.dll", CallingConvention = CallingConvention.Cdecl)]
		public static extern int CreateFrameFeeder();

	    [DllImport("libFrameFeeder.dll", CallingConvention = CallingConvention.Cdecl)]
	    public static extern void SubscribeToColorFrames(ColorFrameCallback progressCallback);

	    [DllImport("libFrameFeeder.dll", CallingConvention = CallingConvention.Cdecl)]
	    public static extern void UnsubscribeFromColorFrames(ColorFrameCallback progressCallback);

	    [DllImport("libFrameFeeder.dll", CallingConvention = CallingConvention.Cdecl)]
	    public static extern void SubscribeToDepthFrames(DepthFrameCallback progressCallback);

	    [DllImport("libFrameFeeder.dll", CallingConvention = CallingConvention.Cdecl)]
	    public static extern void UnsubscribeFromDepthFrames(DepthFrameCallback progressCallback);

		[DllImport("libFrameFeeder.dll", CallingConvention = CallingConvention.Cdecl)]
		public static extern int DestroyFrameFeeder();

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