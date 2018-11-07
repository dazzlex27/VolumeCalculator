#pragma once

#include "Structures.h"
#include "OpenCVInclude.h"

class ContourExtractor
{
private:
	const int _cannyThreshold1 = 50;
	const int _cannyThreshold2 = 200;

public:
	const Contour ExtractContourFromBinaryImage(const cv::Mat& image) const;
	const Contour ExtractContourFromColorImage(const cv::Mat& image) const;

private:
	const Contour GetContourClosestToCenter(const std::vector<Contour>& contours, const int width, const int height) const;
};