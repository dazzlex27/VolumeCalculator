#pragma once

#include "Structures.h"
#include "CVInclude.h"
#include <vector>

class VolumeChecker
{
private:
	const float _halfFovX;
	const float _halfFovY;
	const int _mapWidth;
	const int _mapHeight;
	const int _mapLength;
	const int _mapLengthBytes;
	const int _cutOffDepth;

	short* _mapBuffer;
	byte* _imgBuffer;
	ObjDimDescription* _result;

public:
	VolumeChecker(const float fovX, const float fovY, const int mapWidth, const int mapHeight, const int cutOffDepth);
	~VolumeChecker();

	ObjDimDescription* GetVolume(const short*const mapData);

private:
	const short GetAverageAreaValue(const std::vector<short>& values);
	const AbsRect CalculatePlaneSizeAtGivenHeight(const short height);
	const Contour GetLargestContour();
};