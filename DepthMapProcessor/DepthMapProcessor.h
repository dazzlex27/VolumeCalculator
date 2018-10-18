#pragma once

#include "Structures.h"
#include "OpenCVInclude.h"

class DepthMapProcessor
{
private:
	const ColorCameraIntristics _colorIntrinsics; 
	const DepthCameraIntristics _depthIntrinsics;

	int _colorImageWidth;
	int _colorImageHeight;
	int _colorImageLength;
	int _colorImageLengthBytes;
	int _mapWidth;
	int _mapHeight;
	int _mapLength;
	int _mapLengthBytes;
	short _floorDepth;
	short _cutOffDepth;

	ObjDimDescription _result;
	byte* _colorImageBuffer;
	short* _mapBuffer;
	byte* _imgBuffer;

public:
	DepthMapProcessor(ColorCameraIntristics colorIntrinsics, DepthCameraIntristics depthIntrinsics);
	~DepthMapProcessor();

	ObjDimDescription* CalculateObjectVolume(const int mapWidth, const int mapHeight, const short*const mapData);
	ObjDimDescription* CalculateObjectVolumeAlt(const int imageWidth, const int imageHeight, const byte*const imageData, 
		const int bytesPerPixel, const int mapWidth, const int mapHeight, const short*const mapData);
	const short CalculateFloorDepth(const int mapWidth, const int mapHeight, const short*const mapData);
	void SetSettings(const short floorDepth, const short cutOffDepth);

private:
	void ResizeColorBuffer(const int imageWidth, const int imageHeight, const int bytesPerPixel);
	void ResizeDepthBuffers(const int mapWidth, const int mapHeight);
	const short GetContourTopPlaneDepth(const Contour& contour, const cv::RotatedRect& rotBoundingRect) const;
	const Contour GetTargetContour(const short*const mapBuffer, const int mapNum = 0) const;
	const ObjDimDescription CalculateContourDimensions(const Contour& contour) const;
	const Contour GetContourClosestToCenter(const std::vector<Contour>& contours) const;
	const short FindModeInSortedArray(const short*const array, const int count) const;
	void DrawTargetContour(const Contour& contour, const int contourNum) const;
};