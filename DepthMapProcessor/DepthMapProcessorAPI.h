#pragma once

#include "Structures.h"

#define DLL_EXPORT extern "C" _declspec(dllexport)

DLL_EXPORT void* CreateDepthMapProcessor(float focalLengthX, float focalLengthY, float principalX,
	float principalY, short minDepth, short maxDepth);

DLL_EXPORT void SetCalculatorSettings(void* handle, short floorDepth, short cutOffDepth);

DLL_EXPORT ObjDimDescription* CalculateObjectVolume(void* handle, int mapWidth, int mapHeight, short* mapData);

DLL_EXPORT short CalculateFloorDepth(void* handle, int mapWidth, int mapHeight, short* mapData);

DLL_EXPORT void DestroyDepthMapProcessor(void* handle);