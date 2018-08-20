#pragma once

#include "VolumeChecker.h"

void CreateVolumeChecker(const float fovX, const float fovY, int mapWidth, int mapHeight, int cutOffDepth);

ObjDimDescription* CheckVolume(short* mapData);

void DestroyVolumeChecker();