using System.Runtime.InteropServices;
using VolumeCheckerGUI.Entities;

namespace VolumeCheckerGUI.Utils
{
    internal static class DllWrapper
    {
		[DllImport("libframefeeder.dll", CallingConvention = CallingConvention.Cdecl)]
		public static extern int CreateFrameFeeder();

	    [UnmanagedFunctionPointer(CallingConvention.StdCall)]
	    public unsafe delegate void ColorFrameCallback(ColorFrame* frame);

		[UnmanagedFunctionPointer(CallingConvention.StdCall)]
	    public unsafe delegate void DepthFrameCallback(DepthFrame* frame);

	    [DllImport("libframefeeder.dll", CallingConvention = CallingConvention.Cdecl)]
	    public static extern void SubscribeToColorFrames(ColorFrameCallback progressCallback);

	    [DllImport("libframefeeder.dll", CallingConvention = CallingConvention.Cdecl)]
	    public static extern void UnsubscribeFromColorFrames(ColorFrameCallback progressCallback);

	    [DllImport("libframefeeder.dll", CallingConvention = CallingConvention.Cdecl)]
	    public static extern void SubscribeToDepthFrames(DepthFrameCallback progressCallback);

	    [DllImport("libframefeeder.dll", CallingConvention = CallingConvention.Cdecl)]
	    public static extern void UnsubscribeFromDepthFrames(DepthFrameCallback progressCallback);

		[DllImport("libframefeeder.dll", CallingConvention = CallingConvention.Cdecl)]
		public static extern int DestroyFrameFeeder();

		[DllImport("libvolumeChecker.dll", CallingConvention = CallingConvention.Cdecl)]
		public static extern int CreateVolumeChecker(float fovX, float fovY);

		[DllImport("libvolumeChecker.dll", CallingConvention = CallingConvention.Cdecl)]
		public static extern unsafe ObjDimDescription* CalculateVolume(int mapWidth, int mapHeight, short* mapData);

	    [DllImport("libvolumeChecker.dll", CallingConvention = CallingConvention.Cdecl)]
	    public static extern unsafe short CalculateFloorDepth(int mapWidth, int mapHeight, short* mapData);

		[DllImport("libvolumeChecker.dll", CallingConvention = CallingConvention.Cdecl)]
	    public static extern void SetCheckerSettings(short minDepth, short floorDepth, short cutOffDepth);

		[DllImport("libvolumeChecker.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern int DestroyVolumeChecker();
    }
}