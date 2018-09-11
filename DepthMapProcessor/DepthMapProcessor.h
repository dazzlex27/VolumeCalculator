#pragma once

#include "Structures.h"
#include "OpenCVInclude.h"

class DepthMapProcessor
{
private:
	const float _halfFovX;
	const float _halfFovY;
	int _mapWidth;
	int _mapHeight;
	int _mapLength;
	int _mapLengthBytes;
	short _minDepth;
	short _floorDepth;
	short _cutOffDepth;

	short* _mapBuffer;
	short* _mapBuffer2;
	byte* _imgBuffer;
	ObjDimDescription* _result;

public:
	DepthMapProcessor(const float fovX, const float fovY);
	~DepthMapProcessor();

	ObjDimDescription* CalculateObjectVolume(const int mapWidth, const int mapHeight, const short*const mapData);
	short CalculateFloorDepth(const int mapWidth, const int mapHeight, const short*const mapData);
	void SetSettings(const short minDepth, const short floorDepth, const short cutOffDepth);

private:
	void ResizeBuffers(const int mapWidth, const int mapHeight);
	const short GetAverageAreaValue(const std::vector<short>& values);
	const AbsRect CalculatePlaneSizeAtGivenHeight(const short height);
	const Contour GetTargetContour(const short*const mapBuffer, const int mapNum = 0);
	const Contour GetContourClosestToCenter(const std::vector<Contour>& contours);
	void DrawTargetContour(const Contour& contour, const int contourNum);
};