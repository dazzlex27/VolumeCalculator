#pragma once

#include "Structures.h"

#define DLL_EXPORT extern "C" _declspec(dllexport)

DLL_EXPORT int CreateDepthMapProcessor(const float fovX, const float fovY);

DLL_EXPORT void SetCalculatorSettings(short minDepth, short floorDepth, short cutOffDepth);

DLL_EXPORT ObjDimDescription* CalculateObjectVolume(int mapWidth, int mapHeight, short* mapData);

DLL_EXPORT short CalculateFloorDepth(int mapWidth, int mapHeight, short* mapData);

DLL_EXPORT ObjDimDescription* CalculateVolumeFromStereo(int mapWidth, int mapHeight, short* mapData1, short* mapData2, int offsetXmm, int offsetYmm);

DLL_EXPORT int DestroyDepthMapProcessor();