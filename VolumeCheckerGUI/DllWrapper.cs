using System.Runtime.InteropServices;
using VolumeCheckerGUI.Structures;

namespace VolumeCheckerGUI
{
    internal static class DllWrapper
    {
		[DllImport("libframefeeder.dll", CallingConvention = CallingConvention.Cdecl)]
		public static extern int CreateFrameFeeder();

		[DllImport("libframefeeder.dll", CallingConvention = CallingConvention.Cdecl)]
		public static extern bool IsDeviceAvailable();

		[DllImport("libframefeeder.dll", CallingConvention = CallingConvention.Cdecl)]
		public static extern unsafe ColorFrame* GetNextRgbFrame();

		[DllImport("libframefeeder.dll", CallingConvention = CallingConvention.Cdecl)]
		public static extern unsafe DepthFrame* GetNextDepthFrame();

		[DllImport("libframefeeder.dll", CallingConvention = CallingConvention.Cdecl)]
		public static extern int DestroyFrameFeeder();

		[DllImport("libvolumeChecker.dll", CallingConvention = CallingConvention.Cdecl)]
		public static extern int CreateVolumeChecker(float fovX, float fovY);

		[DllImport("libvolumeChecker.dll", CallingConvention = CallingConvention.Cdecl)]
		public static extern unsafe ObjDimDescription* CheckVolume(int mapWidth, int mapHeight, short* mapData);

		[DllImport("libvolumeChecker.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern int DestroyVolumeChecker();
    }
}