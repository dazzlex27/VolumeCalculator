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

	std::string _debugPath;

	ObjDimDescription _result;
	short* _depthMapBuffer;
	byte* _depthMaskBuffer;
	byte* _colorImageBuffer;

	std::vector<cv::Point2f> _polygonPoints;
	MeasurementVolume _measurementVolume;
	bool _needToUpdateMeasurementVolume;

public:
	DepthMapProcessor(ColorCameraIntristics colorIntrinsics, DepthCameraIntristics depthIntrinsics);
	~DepthMapProcessor();

	void SetAlgorithmSettings(const short floorDepth, const short cutOffDepth, 
		const RelPoint* polygonPoints, const int polygonPointCount, const RelRect& roiRect);
	void SetDebugPath(const char* path);
	ObjDimDescription* CalculateObjectVolume(const DepthMap& depthMap, const bool applyPerspective, const bool saveDebugData);
	ObjDimDescription* CalculateObjectVolumeAlt(const DepthMap& depthMap, const ColorImage& image, const bool applyPerspective, 
		const bool saveDebugData);
	const short CalculateFloorDepth(const DepthMap& depthMap);

private:
	void FillColorBufferFromImage(const ColorImage& image);
	void FillDepthBufferFromDepthMap(const DepthMap& depthMap);
	const Contour GetTargetContourFromDepthMap(const bool saveDebugData) const;
	const Contour GetTargetContourFromColorImage(const bool saveDebugData) const;
	const ObjDimDescription CalculateContourDimensions(const Contour& contour, const bool applyPerspective, const bool saveDebugData) const;
	const cv::RotatedRect CalculateObjectBoundingRect(const Contour& depthObjectContour, const short contourTopPlaneDepth,
		const bool applyPerspective, const bool saveDebugData) const;
	const ObjDimDescription CalculateContourDimensionsAlt(const Contour& objectContour, const Contour& colorObjectContour,
		const bool applyPerspective, const bool saveDebugData) const;
	const short GetContourTopPlaneDepth(const Contour& contour) const;
	const TwoDimDescription GetTwoDimDescription(const cv::RotatedRect& contourBoundingRect,
		const short contourTopPlaneDepth, const float fx, const float fy, const float ppx, const float ppy) const;
	const std::vector<DepthValue> GetWorldDepthValues(const Contour& objectContour) const;
	const std::vector<cv::Point> GetCameraPoints(const std::vector<DepthValue>& depthValues, const short targetDepth) const;
	const bool IsObjectInZone(const std::vector<DepthValue>& contour) const;
	void UpdateMeasurementVolume(const int mapWidth, const int mapHeight);
	bool IsPointInsidePolygon(const std::vector<cv::Point>& polygon, int x, int y);
	const std::vector<DepthValue> GetWorldDepthValuesFromDepthMap();
};