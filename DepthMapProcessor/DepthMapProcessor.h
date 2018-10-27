#pragma once

#include "Structures.h"
#include "OpenCVInclude.h"

class DepthMapProcessor
{
private:
	const int _cannyThreshold1 = 50;
	const int _cannyThreshold2 = 200;

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
		const int bytesPerPixel, const RelRect& roiRect, const int mapWidth, const int mapHeight, const short*const mapData);
	const short CalculateFloorDepth(const int mapWidth, const int mapHeight, const short*const mapData);
	void SetSettings(const short floorDepth, const short cutOffDepth);

private:
	void ResizeColorBuffer(const int imageWidth, const int imageHeight, const int bytesPerPixel);
	void ResizeDepthBuffers(const int mapWidth, const int mapHeight);
	const Contour GetTargetContourFromDepthMap() const;
	const Contour GetTargetContourFromColorFrame(const int bytesPerPixel, const RelRect& roiRect) const;
	const ObjDimDescription CalculateContourDimensions(const Contour& contour) const;
	const ObjDimDescription CalculateContourDimensionsAlt(const Contour& objectContour, const Contour& colorObjectContour) const;
	const Contour GetContourClosestToCenter(const std::vector<Contour>& contours) const;
	const short GetContourTopPlaneDepth(const Contour& contour, const cv::RotatedRect& rotBoundingRect) const;
	const TwoDimDescription GetTwoDimDescription(const cv::RotatedRect& contourBoundingRect, 
		const short contourTopPlaneDepth, const float fx, const float fy, const float ppx, const float ppy) const;
};