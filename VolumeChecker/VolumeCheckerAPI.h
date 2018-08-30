#pragma once

#include "Structures.h"

#define DLL_EXPORT extern "C" _declspec(dllexport)

DLL_EXPORT int TestExport();

DLL_EXPORT int CreateVolumeChecker(const float fovX, const float fovY, int mapWidth, int mapHeight, int floorDepth, int cutOffDepth);

DLL_EXPORT ImageFrame* GetNextRgbFrame();

DLL_EXPORT DepthFrame* GetNextDepthFrame();

DLL_EXPORT ObjDimDescription* CheckVolume(short* mapData);

DLL_EXPORT int DestroyVolumeChecker();