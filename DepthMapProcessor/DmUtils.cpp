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

void DmUtils::FilterDepthMapByMaxDepth(const int mapDataLength, short*const mapData,  const short value)
{
	for (int i = 0; i < mapDataLength; i++)
	{
		if (mapData[i] > value)
			mapData[i] = 0;
	}
}

void DmUtils::FilterDepthMapByMeasurementVolume(short*const mapData, const std::vector<DepthValue>& worldDepthValues, 
	const MeasurementVolume& volume)
{
	for (int i = 0; i < worldDepthValues.size(); i++)
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

void DmUtils::DrawTargetContour(const Contour& contour, const int width, const int height, const std::string& debugPath, 
	const std::string& contourLabel)
{
	cv::RotatedRect rect = cv::minAreaRect(cv::Mat(contour));
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

	cv::Mat img2 = cv::Mat::zeros(height, width, CV_8UC3);

	cv::Scalar colors[3];
	colors[0] = cv::Scalar(255, 0, 0);
	colors[1] = cv::Scalar(0, 255, 0);
	for (auto i = 0; i < contoursToDraw.size(); i++)
		cv::drawContours(img2, contoursToDraw, i, colors[i]);

	const std::string& index = GetCurrentCalculationIndex();

	cv::imwrite(debugPath + "/" + index + "_" + contourLabel + ".png", img2);
}

std::string DmUtils::GetCurrentCalculationIndex()
{
	std::string index;

	std::ifstream countersFile;
	countersFile.open("counters");
	if (!countersFile.good())
		return "0";

	bool isEmpty = countersFile.peek() == std::ifstream::traits_type::eof();
	if (isEmpty)
		return "0";

	countersFile >> index;

	return index;
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

// Source:  https://wrf.ecse.rpi.edu//Research/Short_Notes/pnpoly.html
bool DmUtils::IsPointInsidePolygon(const std::vector<cv::Point>& polygon, int x, int y)
{
	bool pointIsInPolygon = false;

	for (int i = 0, j = (int)(polygon.size() - 1); i < (int)polygon.size(); j = i++)
	{
		bool pointInLineScope = (polygon[i].y > y) != (polygon[j].y > y);

		int lineXHalf = (polygon[j].x - polygon[i].x) * (y - polygon[i].y) / (polygon[j].y - polygon[i].y);
		int lineXPosition = lineXHalf + polygon[i].x;
		bool pointInLeftHalfPlaneOfLine = x < lineXPosition;

		if (pointInLineScope && pointInLeftHalfPlaneOfLine)
			pointIsInPolygon = !pointIsInPolygon;
	}

	return pointIsInPolygon;
}