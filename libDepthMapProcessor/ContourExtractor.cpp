#include "ContourExtractor.h"
#include "ProcessingUtils.h"

const Contour ContourExtractor::ExtractContourFromBinaryImage(const cv::Mat& image, const bool saveDebugData) const
{
	std::vector<Contour> contours;
	cv::findContours(image, contours, CV_RETR_EXTERNAL, CV_CHAIN_APPROX_SIMPLE);

	const std::vector<Contour>& validContours = DmUtils::GetValidContours(contours, 0.0001f, image.cols * image.rows);

	if (validContours.size() == 0)
		return Contour();

	return GetContourClosestToCenter(validContours, image.cols, image.rows);
}

const Contour ContourExtractor::ExtractContourFromColorImage(const cv::Mat& image, const bool saveDebugData) const
{
	cv::Mat cannied;
	cv::Canny(image, cannied, _cannyThreshold1, _cannyThreshold2);

	std::vector<Contour> contours;
	cv::findContours(cannied, contours, CV_RETR_EXTERNAL, CV_CHAIN_APPROX_SIMPLE);

	Contour mergedContour;
	for (int i = 0; i < contours.size(); i++)
	{
		for (int j = 0; j < contours[i].size(); j++)
			mergedContour.emplace_back(contours[i][j]);
	}

	if (saveDebugData)
	{
		const std::string& index = DmUtils::GetCurrentCalculationIndex();
		cv::imwrite(_debugPath + "/" + index + "_cannied.png", cannied);
	}

	return mergedContour;
}

void ContourExtractor::SetDebugPath(const std::string& path)
{
	_debugPath = std::string(path);
}

const Contour ContourExtractor::GetContourClosestToCenter(const std::vector<Contour>& contours, const int width, const int height) const
{
	if (contours.size() == 0)
		return Contour();

	if (contours.size() == 1)
		return contours[0];

	const int centerX = width / 2;
	const int centerY = height / 2;

	float resultDistanceToCenter = (float)INT32_MAX;
	Contour closestToCenterContour;

	for (uint i = 0; i < contours.size(); i++)
	{
		const cv::Moments& m = cv::moments(contours[i]);
		const int cx = (int)(m.m10 / m.m00);
		const int cy = (int)(m.m01 / m.m00);
		const float distanceToCenter = DmUtils::GetDistanceBetweenPoints(centerX, centerY, cx, cy);

		if (distanceToCenter >= resultDistanceToCenter)
			continue;

		resultDistanceToCenter = distanceToCenter;
		closestToCenterContour = contours[i];
	}

	return closestToCenterContour;
}