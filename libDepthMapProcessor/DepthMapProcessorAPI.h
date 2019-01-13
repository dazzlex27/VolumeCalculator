#pragma once

#include "Structures.h"

#define DLL_EXPORT extern "C" _declspec(dllexport)

DLL_EXPORT void* CreateDepthMapProcessor(ColorCameraIntristics colorIntrinsics, DepthCameraIntristics depthIntrinsics);

DLL_EXPORT void SetAlgorithmSettings(void* handle, short floorDepth, short cutOffDepth, RelPoint* polygonPoints, int polygonPointCount,
	RelRect colorRoiRect);

DLL_EXPORT void SetDebugPath(void* handle, const char* path);

DLL_EXPORT ObjDimDescription* CalculateObjectVolume(void* handle, DepthMap depthMap, bool saveDebugData);

DLL_EXPORT ObjDimDescription* CalculateObjectVolumeAlt(void* handle, DepthMap depthMap, ColorImage image, bool saveDebugData);

DLL_EXPORT short CalculateFloorDepth(void* handle, DepthMap depthMap);

DLL_EXPORT void DestroyDepthMapProcessor(void* handle);