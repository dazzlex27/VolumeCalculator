#include "DmUtils.h"
#include <cmath>
#include <climits>
#include <fstream>

const std::vector<Contour> DmUtils::GetValidContours(const std::vector<Contour>& contours, const float minAreaRatio, 
	const int imageDataLength)
{
	std::vector<Contour> contoursValid;

	if (contours.size() == 0)
		return contoursValid;

	contoursValid.reserve(contours.size());
	const int minContourAreaPixels = (const int)(imageDataLength * minAreaRatio);
	for (int i = 0; i < contours.size(); i++)
	{
		if (contourArea(contours[i]) >= minContourAreaPixels)
			contoursValid.emplace_back(Contour(contours[i]));
	}

	return contoursValid;
}

const RelPoint DmUtils::AbsoluteToRelative(const cv::Point& abs, const int width, const int height)
{
	RelPoint res{};
	res.X = (float)(abs.x) / width;
	res.Y = (float)(abs.y) / height;

	return res;
}

void DmUtils::ConvertDepthMapDataToBinaryMask(const int mapDataLength, const short*const mapData, byte*const maskData)
{
	for (int i = 0; i < mapDataLength; i++)
	{
		short mapValue = mapData[i];
		maskData[i] = mapValue > 0 ? 255 : 0;
	}
}

void DmUtils::FilterDepthMapByMaxDepth(const int mapDataLength, short*const mapData,  const short value)
{
	for (int i = 0; i < mapDataLength; i++)
	{
		if (mapData[i] > value)
			mapData[i] = 0;
	}
}

void DmUtils::FilterDepthMapByMeasurementVolume(const int mapLength, short*const mapData, DepthValue*const worldDepthValues, 
	const MeasurementVolume& volume)
{
	for (int i = 0; i < mapLength; i++)
	{
		const bool pointIsInZone = IsPointInZone(worldDepthValues[i], volume);
		if (!pointIsInZone)
			mapData[i] = 0;
	}
}

const std::vector<short> DmUtils::GetNonZeroContourDepthValues(const DepthMap& depthMap)
{
	std::vector<short> nonZeroValues;
	const int mapLength = depthMap.Width * depthMap.Height;
	if (mapLength <= 0)
		return nonZeroValues;

	nonZeroValues.reserve(mapLength);

	for (int i = 0; i < mapLength; i++)
	{
		const short value = depthMap.Data[i];
		if (value > 0)
			nonZeroValues.emplace_back(value);
	}

	return nonZeroValues;
}

const std::vector<short> DmUtils::GetNonZeroContourDepthValues(const int mapWidth, const int mapHeight, 
	const short*const mapData, const cv::RotatedRect& roi, const Contour& contour)
{
	std::vector<short> nonZeroValues;
	if (contour.size() == 0)
		return nonZeroValues;

	const cv::Rect& boundingRect = roi.boundingRect();

	nonZeroValues.reserve(boundingRect.width * boundingRect.height);

	for (int j = boundingRect.y; j < boundingRect.y + boundingRect.height; j++)
	{
		for (int i = boundingRect.x; i < boundingRect.x + boundingRect.width; i++)
		{
			const short value = mapData[j * mapWidth + i];
			const bool valueIsInContour = cv::pointPolygonTest(contour, cv::Point(i, j), false) >= 0.0;
			if (valueIsInContour && value > 0)
				nonZeroValues.emplace_back(value);
		}
	}

	return nonZeroValues;
}

const float DmUtils::GetDistanceBetweenPoints(const int x1, const int y1, const int x2, const int y2)
{
	return (float)sqrt(pow(x1 - x2, 2) + pow(y1 - y2, 2));
}

const cv::Rect DmUtils::GetAbsRoiFromRoiRect(const RelRect& roiRect, const cv::Size& frameSize)
{
	const int x1Abs = (int)(roiRect.X * frameSize.width);
	const int y1Abs = (int)(roiRect.Y * frameSize.height);
	const int widthAbs = (int)(roiRect.Width * frameSize.width);
	const int heightAbs = (int)(roiRect.Height * frameSize.height);

	return cv::Rect(x1Abs, y1Abs, widthAbs, heightAbs);
}

const int DmUtils::GetCvChannelsCodeFromBytesPerPixel(const int bytesPerPixel)
{
	switch (bytesPerPixel)
	{
	case 1:
		return CV_8UC1;
	case 3:
		return CV_8UC3;
	case 4:
		return CV_8UC4;
	default:
		return 0;
	}
}

const short DmUtils::FindModeInSortedArray(const short * const array, const int count)
{
	if (count == 0)
		return 0;

	int mode = array[0];
	int currentCount = 1;
	int currentMax = 1;
	for (int i = 1; i < count; i++)
	{
		if (array[i] == array[i - 1])
		{
			currentCount++;
			if (currentCount <= currentMax)
				continue;

			currentMax = currentCount;
			mode = array[i];
		}
		else
			currentCount = 1;
	}

	return mode;
}

void DmUtils::DrawTargetContour(const Contour& contour, const int width, const int height, const std::string& filename)
{
	const cv::RotatedRect& rect = cv::minAreaRect(cv::Mat(contour));
	cv::Point2f points[4];
	rect.points(points);

	Contour rectContour;
	rectContour.emplace_back(points[0]);
	rectContour.emplace_back(points[1]);
	rectContour.emplace_back(points[2]);
	rectContour.emplace_back(points[3]);

	std::vector<Contour> contoursToDraw;
	contoursToDraw.emplace_back(contour);
	contoursToDraw.emplace_back(rectContour);

	const cv::Mat& img2 = cv::Mat::zeros(height, width, CV_8UC3);

	cv::Scalar colors[3];
	colors[0] = cv::Scalar(255, 0, 0);
	colors[1] = cv::Scalar(0, 255, 0);
	for (auto i = 0; i < contoursToDraw.size(); i++)
		cv::drawContours(img2, contoursToDraw, i, colors[i]);

	cv::imwrite(filename, img2);
}

bool DmUtils::IsPointInZone(const DepthValue& worldPoint, const MeasurementVolume& volume)
{
	if (volume.Points.size() == 0)
		return false;

	if (worldPoint.Value > volume.largerDepthValue)
		return false;

	if (worldPoint.Value < volume.smallerDepthValue)
		return false;

	double isInside = cv::pointPolygonTest(volume.Points, cv::Point(worldPoint.XWorld, worldPoint.YWorld), false);
	if (isInside < 0)
		return false;

	return true;
}

bool DmUtils::IsObjectInBounds(const Contour& objectContour, const int width, const int height)
{
	const int borderDistance = 3;
	const cv::RotatedRect& colorObjectBoundingRect = cv::minAreaRect(objectContour);
	const bool rectLowerXIsOk = colorObjectBoundingRect.center.x > (colorObjectBoundingRect.size.width / 2 + borderDistance);
	const bool rectUpperXIsOk = colorObjectBoundingRect.center.x < (width - colorObjectBoundingRect.size.width / 2 - borderDistance);
	const bool rectLowerYIsOk = colorObjectBoundingRect.center.y > (colorObjectBoundingRect.size.height / 2 + borderDistance);
	const bool rectUpperYIsOk = colorObjectBoundingRect.center.y < (height - colorObjectBoundingRect.size.height / 2 - borderDistance);

	return rectLowerXIsOk && rectUpperXIsOk && rectLowerYIsOk && rectUpperYIsOk;
}
