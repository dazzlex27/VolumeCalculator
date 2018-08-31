#pragma once

#include "Structures.h"

#define DLL_EXPORT extern "C" _declspec(dllexport)

DLL_EXPORT int CreateVolumeChecker(const float fovX, const float fovY);

DLL_EXPORT void SetCheckerSettings(short floorDepth, short cutOffDepth);

DLL_EXPORT ObjDimDescription* CheckVolume(int mapWidth, int mapHeight, short* mapData);

DLL_EXPORT int DestroyVolumeChecker();