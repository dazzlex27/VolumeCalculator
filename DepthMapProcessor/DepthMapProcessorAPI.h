#pragma once

#include "Structures.h"

#define DLL_EXPORT extern "C" _declspec(dllexport)

DLL_EXPORT void CreateDepthMapProcessor(CameraIntrinsics colorIntrinsics, CameraIntrinsics depthIntrinsics);

DLL_EXPORT void SetAlgorithmSettings(short floorDepth, short cutOffDepth, RelPoint* polygonPoints, int polygonPointCount,
	RelRect colorRoiRect);

DLL_EXPORT void SetDebugPath(const char* path);

DLL_EXPORT int SelectAlgorithm(DepthMap depthMap, ColorImage colorImage, const long measuredDistance,
	bool dm1Enabled, bool dm2Enabled, bool rgbEnabled);

DLL_EXPORT VolumeCalculationResult* CalculateObjectVolume(VolumeCalculationData calculationData);

DLL_EXPORT void DisposeCalculationResult(VolumeCalculationResult* result);

DLL_EXPORT short CalculateFloorDepth(DepthMap depthMap);

DLL_EXPORT void DestroyDepthMapProcessor();