#pragma once

#include "Structures.h"
#include "OpenCVInclude.h"

class DepthMapProcessor
{
private:
	const float _focalLengthX;
	const float _focalLengthY;
	const float _principalX;
	const float _principalY;
	const short _minDepth;
	const short _maxDepth;

	int _mapWidth;
	int _mapHeight;
	int _mapLength;
	int _mapLengthBytes;
	short _floorDepth;
	short _cutOffDepth;

	ObjDimDescription _result;
	short* _mapBuffer;
	byte* _imgBuffer;

public:
	DepthMapProcessor(const float focalLengthX, const float focalLengthY, const float principalX,
		const float principalY, const short minDepth, const short maxDepth);
	~DepthMapProcessor();

	ObjDimDescription* CalculateObjectVolume(const int mapWidth, const int mapHeight, const short*const mapData);
	const short CalculateFloorDepth(const int mapWidth, const int mapHeight, const short*const mapData);
	void SetSettings(const short floorDepth, const short cutOffDepth);

private:
	void ResizeBuffers(const int mapWidth, const int mapHeight);
	const short GetContourTopPlaneDepth(const Contour& contour, const cv::RotatedRect& rotBoundingRect) const;
	const Contour GetTargetContour(const short*const mapBuffer, const int mapNum = 0) const;
	const ObjDimDescription CalculateContourDimensions(const Contour& contour) const;
	const Contour GetContourClosestToCenter(const std::vector<Contour>& contours) const;
	const short FindModeInSortedArray(const short*const array, const int count) const;
	void DrawTargetContour(const Contour& contour, const int contourNum) const;
};