#pragma once

#include "Structures.h"

#define DLL_EXPORT extern "C" _declspec(dllexport)

DLL_EXPORT void* CreateDepthMapProcessor(ColorCameraIntristics colorIntrinsics, DepthCameraIntristics depthIntrinsics);

DLL_EXPORT void SetAlgorithmSettings(void* handle, short floorDepth, short cutOffDepth, RelPoint* polygonPoints, int polygonPointCount,
	RelRect colorRoiRect);

DLL_EXPORT void SetDebugPath(void* handle, const char* path);

DLL_EXPORT ObjDimDescription* CalculateObjectVolume(void* handle, DepthMap depthMap, long distance, bool applyPerspective, 
	bool saveDebugData, bool maskMode);

DLL_EXPORT ObjDimDescription* CalculateObjectVolumeAlt(void* handle, DepthMap depthMap, ColorImage image, long distance,
	bool applyPerspective, bool saveDebugData, bool maskMode);

DLL_EXPORT int SelectAlgorithm(void* handle, DepthMap depthMap, ColorImage colorImage, const long measuredDistance,
	bool dm1Enabled, bool dm2Enabled, bool rgbEnabled);

DLL_EXPORT short CalculateFloorDepth(void* handle, DepthMap depthMap);

DLL_EXPORT void DestroyDepthMapProcessor(void* handle);