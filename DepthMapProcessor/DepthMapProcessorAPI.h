#pragma once

#include "Structures.h"

#define DLL_EXPORT extern "C" _declspec(dllexport)

DLL_EXPORT void* CreateDepthMapProcessor(ColorCameraIntristics colorIntrinsics, DepthCameraIntristics depthIntrinsics);

DLL_EXPORT void SetCalculatorSettings(void* handle, short floorDepth, short cutOffDepth);

DLL_EXPORT ObjDimDescription* CalculateObjectVolume(void* handle, int mapWidth, int mapHeight, short* mapData);

DLL_EXPORT ObjDimDescription* CalculateObjectVolumeAlt(void* handle, int imageWidth, int imageHeight, byte* imageData, int bytesPerPixel,
	float x1, float y1, float x2, float y2, int mapWidth, int mapHeight, short* mapData);

DLL_EXPORT short CalculateFloorDepth(void* handle, int mapWidth, int mapHeight, short* mapData);

DLL_EXPORT void DestroyDepthMapProcessor(void* handle);