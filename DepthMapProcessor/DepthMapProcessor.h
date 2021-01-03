#pragma once

#include "Structures.h"
#include "OpenCVInclude.h"
#include "ContourExtractor.h"

class DepthMapProcessor
{
private:
	const CameraIntrinsics _colorIntrinsics;
	const CameraIntrinsics _depthIntrinsics;

	const short _maxObjHeightForRgb = 300; // objects with height of 300mm and less are ok for rgb calculation
	const short _contourPlaneDepthDeltaForDm2 = 100; // if object is taller than 100mm - use dm2, dm1 - otherwise

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

	bool _maskMode;

	std::string _debugPath;

	short* _depthMapBuffer;
	byte* _depthMaskBuffer;
	byte* _colorImageBuffer;

	std::vector<cv::Point2f> _polygonPoints;
	MeasurementVolume _measurementVolume;
	bool _needToUpdateMeasurementVolume;

public:
	DepthMapProcessor(CameraIntrinsics colorIntrinsics, CameraIntrinsics depthIntrinsics);
	~DepthMapProcessor();

	void SetAlgorithmSettings(const short floorDepth, const short cutOffDepth, 
		const RelPoint* polygonPoints, const int polygonPointCount, const RelRect& roiRect);
	void SetDebugPath(const char* path, const bool maskMode);

	NativeAlgorithmSelectionResult* SelectAlgorithm(const NativeAlgorithmSelectionData data);
	VolumeCalculationResult* CalculateObjectVolume(const VolumeCalculationData& data);
	void PrepareBuffers(const DepthMap*const depthMap, const ColorImage*const colorImage);
	const short CalculateFloorDepth(const DepthMap& depthMap);

private:
	void FillColorBufferFromImage(const ColorImage& image);
	void FillDepthBufferFromDepthMap(const DepthMap& depthMap);
	const Contour GetTargetContourFromDepthMap() const;
	const Contour GetTargetContourFromColorImage(const char* debugPath = "") const;
	const TwoDimDescription Calculate2DContourDimensions(const Contour& depthObjectContour,
		const Contour& colorObjectContour, const AlgorithmSelectionStatus selectedAlgorithm, const short contourTopPlaneDepth) const;
	const cv::RotatedRect CalculateObjectBoundingRect(const Contour& depthObjectContour, const Contour& colorObjectContour,
		const AlgorithmSelectionStatus selectedAlgorithm, const short contourTopPlaneDepth, const char* debugPath = "") const;
	const ContourPlanes GetDepthContourPlanes(const Contour& contour) const;
	const TwoDimDescription GetTwoDimDescription(const cv::RotatedRect& contourBoundingRect,
		const CameraIntrinsics& intristics, const short contourTopPlaneDepth) const;
	const bool IsObjectInZone(const std::vector<DepthValue>& contour) const;
	void UpdateMeasurementVolume(const int mapWidth, const int mapHeight);

	void Tamper(VolumeCalculationResult* result) const;
};