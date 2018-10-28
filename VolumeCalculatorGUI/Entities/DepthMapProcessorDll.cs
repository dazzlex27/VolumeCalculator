﻿using System.Runtime.InteropServices;
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
	    public static extern unsafe ObjDimDescription* CalculateObjectVolumeAlt(void* handle, int imageWidth, int imageHeight, byte* imageData, 
		    int bytesPerPixel, float x1, float y1, float x2, float y2, int mapWidth, int mapHeight, short* depthData);

		[DllImport("libDepthMapProcessor.dll", CallingConvention = CallingConvention.Cdecl)]
	    public static extern unsafe short CalculateFloorDepth(void* handle, int mapWidth, int mapHeight, short* mapData);

		[DllImport("libDepthMapProcessor.dll", CallingConvention = CallingConvention.Cdecl)]
	    public static extern unsafe void SetCalculatorSettings(void* handle, short floorDepth, short cutOffDepth);

		[DllImport("libDepthMapProcessor.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern unsafe void DestroyDepthMapProcessor(void* handle);
    }
}