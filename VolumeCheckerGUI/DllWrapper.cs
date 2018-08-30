using System.Runtime.InteropServices;
using VolumeCheckerGUI.Structures;

namespace VolumeCheckerGUI
{
    internal static class DllWrapper
    {
        [DllImport("libvolumeChecker.dll", CallingConvention = CallingConvention.Cdecl)]
        public extern static int TestExport();

        [DllImport("libvolumeChecker.dll", CallingConvention = CallingConvention.Cdecl)]
        public extern static int CreateVolumeChecker(float fovX, float fovY, int mapWidth, int mapHeight, int floorDepth, int cutOffDepth);

		[DllImport("libvolumeChecker.dll", CallingConvention = CallingConvention.Cdecl)]
		public extern static unsafe ImageFrame* GetNextRgbFrame();

		[DllImport("libvolumeChecker.dll", CallingConvention = CallingConvention.Cdecl)]
		public extern static unsafe DepthFrame* GetNextDepthFrame();

		[DllImport("libvolumeChecker.dll", CallingConvention = CallingConvention.Cdecl)]
		public extern static unsafe ObjDimDescription* CheckVolume(short* mapData);

		[DllImport("libvolumeChecker.dll", CallingConvention = CallingConvention.Cdecl)]
        public extern static int DestroyVolumeChecker();
    }
}