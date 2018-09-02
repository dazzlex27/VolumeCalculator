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
	ObjDimDescription* GetVolumeFromStereo(const int mapWidth, const int mapHeight, const short*const mapData1, const short*const mapData2, const int offsetXmm, const int offsetYmm);
	short CalculateFloorDepth(const int mapWidth, const int mapHeight, const short*const mapData);
	void SetSettings(const short minDepth, const short floorDepth, const short cutOffDepth);

private:
	const short GetAverageAreaValue(const std::vector<short>& values);
	const AbsRect CalculatePlaneSizeAtGivenHeight(const short height);
	const Contour GetLargestContour(const short*const mapBuffer, const int mapNum = 0);
	void ResizeBuffers(const int mapWidth, const int mapHeight);
	const cv::RotatedRect MapSecondContourBoxToFirstImage(const cv::RotatedRect& rect, const AbsRect& planeRect, const int offsetXmm, const int offsetYmm);
};