#pragma once

#include "Structures.h"

#define DLL_EXPORT extern "C" _declspec(dllexport)

DLL_EXPORT int CreateVolumeChecker(const float fovX, const float fovY);

DLL_EXPORT void SetCheckerSettings(short minDepth, short floorDepth, short cutOffDepth);

DLL_EXPORT ObjDimDescription* CheckVolume(int mapWidth, int mapHeight, short* mapData);

DLL_EXPORT ObjDimDescription* CheckVolumeFromStereo(int mapWidth, int mapHeight, short* mapData1, short* mapData2, int offsetXmm, int offsetYmm);

DLL_EXPORT int DestroyVolumeChecker();