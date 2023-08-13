#pragma once

#include "Structures.h"

#define DLL_EXPORT extern "C" _declspec(dllexport)

class DepthMapProcessor;

DLL_EXPORT DepthMapProcessor* CreateDepthMapProcessor(CameraIntrinsics colorIntrinsics, CameraIntrinsics depthIntrinsics);

DLL_EXPORT void SetAlgorithmSettings(DepthMapProcessor* processor, short floorDepth, short cutOffDepth,
	RelPoint* polygonPoints, int polygonPointCount, RelRect colorRoiRect);

DLL_EXPORT void SetDebugDirectory(DepthMapProcessor* processor, const char* path);

DLL_EXPORT NativeAlgorithmSelectionResult* SelectAlgorithm(DepthMapProcessor* processor, NativeAlgorithmSelectionData data);
DLL_EXPORT void DisposeAlgorithmSelectionResult(VolumeCalculationResult* result);

DLL_EXPORT VolumeCalculationResult* CalculateObjectVolume(DepthMapProcessor* processor, VolumeCalculationData data);
DLL_EXPORT void DisposeCalculationResult(VolumeCalculationResult* result);

DLL_EXPORT short CalculateFloorDepth(DepthMapProcessor* processor, DepthMap depthMap);

DLL_EXPORT void DestroyDepthMapProcessor(DepthMapProcessor* processor);
