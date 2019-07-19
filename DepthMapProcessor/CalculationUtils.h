#pragma once

#include <vector>
#include "Structures.h"

class CalculationUtils
{
public:
	static const std::vector<DepthValue> GetWorldDepthValues(const Contour& objectContour, const short*const depthMapBuffer,
		const int mapWidth, const CameraIntrinsics& intrinsics);
	static const std::vector<cv::Point> GetCameraPoints(const std::vector<DepthValue>& depthValues, const short targetDepth,
		const CameraIntrinsics& intrinsics);
	static const std::vector<DepthValue> GetWorldDepthValuesFromDepthMap(const int mapWidth, const int mapHeight,
		const short* const depthMapBuffer, const CameraIntrinsics& intrinsics);
};