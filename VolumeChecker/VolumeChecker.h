#pragma once

#include "Structures.h"
#include "CVInclude.h"
#include <vector>

class VolumeChecker
{
private:
	const float _halfFovX;
	const float _halfFovY;
	int _mapWidth;
	int _mapHeight;
	int _mapLength;
	int _mapLengthBytes;
	int _floorDepth;
	int _cutOffDepth;

	short* _mapBuffer;
	byte* _imgBuffer;
	ObjDimDescription* _result;

public:
	VolumeChecker(const float fovX, const float fovY);
	~VolumeChecker();

	ObjDimDescription* GetVolume(const int mapWidth, const int mapHeight, const short*const mapData);
	void SetSettings(const short floorDepth, const short cutOffDepth);

private:
	const short GetAverageAreaValue(const std::vector<short>& values);
	const AbsRect CalculatePlaneSizeAtGivenHeight(const short height);
	const Contour GetLargestContour();
	void ResizeBuffers(const int mapWidth, const int mapHeight);
};