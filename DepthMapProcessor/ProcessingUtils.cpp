#include "ProcessingUtils.h"
#include <cmath>
#include <climits>
#include <fstream>

const std::vector<Contour> DmUtils::GetValidContours(const std::vector<Contour>& contours, const float minAreaRatio, const int imageDataLength)
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
	RelPoint res;
	res.X = (float)(abs.x) / width;
	res.Y = (float)(abs.y) / height;

	return res;
}

void DmUtils::ConvertDepthMapDataToBinaryMask(const int mapDataLength, const short*const mapData,  byte*const maskData)
{
	for (int i = 0; i < mapDataLength; i++)
	{
		short mapValue = mapData[i];
		maskData[i] = mapValue > 0 ? 255 : 0;
	}
}

void DmUtils::FilterDepthMap(const int mapDataLength, short*const mapData,  const short value)
{
	for (int i = 0; i < mapDataLength; i++)
	{
		if (mapData[i] > value)
			mapData[i] = 0;
	}
}

const std::vector<short> DmUtils::GetNonZeroContourDepthValues(const int mapWidth, const int mapHeight, const short*const mapData)
{
	std::vector<short> nonZeroValues;
	const int mapLength = mapWidth * mapHeight;
	if (mapLength <= 0)
		return nonZeroValues;

	nonZeroValues.reserve(mapLength);

	for (int i = 0; i < mapLength; i++)
	{
		const short value = mapData[i];
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

	cv::Rect boundingRect = roi.boundingRect();


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

const RotRelRect DmUtils::RotAbsRectToRel(const int rotWidth, const int rotHeight, const cv::RotatedRect& rect)
{
	RotRelRect result;

	cv::Point2f rectPoints[4];
	rect.points(rectPoints);

	memcpy(result.Points, rectPoints, 4 * sizeof(FlPoint));
	for (int i = 0; i < 4; i++)
	{
		result.Points[i].X = (float)rectPoints[i].x / rotWidth;
		result.Points[i].Y = (float)rectPoints[i].y / rotHeight;
	}
	result.Width = rect.size.width / rotWidth;
	result.Height = rect.size.height / rotHeight;
	result.AngleDeg = rect.angle;

	return result;
}