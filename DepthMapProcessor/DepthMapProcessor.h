#pragma once

#include "Structures.h"
#include "OpenCVInclude.h"
#include "ContourExtractor.h"

class DepthMapProcessor
{
private:
	const ColorCameraIntristics _colorIntrinsics; 
	const DepthCameraIntristics _depthIntrinsics;
	
	ContourExtractor _contourExtractor;

	int _colorImageWidth;
	int _colorImageHeight;
	int _colorImageBytesPerPixel;
	int _colorImageLengthBytes;
	int _mapWidth;
	int _mapHeight;
	int _mapLength;
	int _mapLengthBytes;
	short _floorDepth;
	short _cutOffDepth;
	bool _correctPerspective;
	RelRect _colorRoiRect;

	ObjDimDescription _result;
	short* _depthMapBuffer;
	byte* _depthMaskBuffer;
	byte* _colorImageBuffer;

public:
	DepthMapProcessor(ColorCameraIntristics colorIntrinsics, DepthCameraIntristics depthIntrinsics);
	~DepthMapProcessor();

	ObjDimDescription* CalculateObjectVolume(const DepthMap& depthMap);
	ObjDimDescription* CalculateObjectVolumeAlt(const DepthMap& depthMap, const ColorImage& image);
	const short CalculateFloorDepth(const DepthMap& depthMap);
	void SetAlgorithmSettings(const short floorDepth, const short cutOffDepth, const RelRect& roiRect);

private:
	void FillColorBuffer(const ColorImage& image);
	void FillDepthBuffer(const DepthMap& depthMap);
	const Contour GetTargetContourFromDepthMap() const;
	const Contour GetTargetContourFromColorImage() const;
	const ObjDimDescription CalculateContourDimensions(const Contour& contour) const;
	const ObjDimDescription CalculateContourDimensionsAlt(const Contour& objectContour, const Contour& colorObjectContour) const;
	const short GetContourTopPlaneDepth(const Contour& contour) const;
	const TwoDimDescription GetTwoDimDescription(const cv::RotatedRect& contourBoundingRect, 
		const short contourTopPlaneDepth, const float fx, const float fy, const float ppx, const float ppy) const;
	const std::vector<DepthValue> GetWorldDepthValues(const Contour& objectContour) const;
	const std::vector<cv::Point> GetCameraPoints(const std::vector<DepthValue>& depthValues, const short targetDepth) const;
};