#include "DepthMapProcessor.h"
#include <string>
#include <cmath>
#include <climits>
#include <ctime>
#include "ProcessingUtils.h"

const float PI = 3.141592653589793238463f;

DepthMapProcessor::DepthMapProcessor(const float focalLengthX, const float focalLengthY, const float principalX,
	const float principalY, const short minDepth, const short maxDepth)
	: _focalLengthX(focalLengthX),
	_focalLengthY(focalLengthY),
	_principalX(principalX),
	_principalY(principalY),
	_minDepth(minDepth),
	_maxDepth(maxDepth)
{
	_mapWidth = 0;
	_mapHeight = 0;
	_mapLength = 0;
	_mapLengthBytes = 0;
}

DepthMapProcessor::~DepthMapProcessor()
{
	delete[] _mapBuffer;
	_mapBuffer = 0;

	delete[] _imgBuffer;
	_imgBuffer = 0;
}

void DepthMapProcessor::SetSettings(const short floorDepth, const short cutOffDepth)
{
	_floorDepth = floorDepth;
	_cutOffDepth = cutOffDepth;
}

ObjDimDescription* DepthMapProcessor::CalculateObjectVolume(const int mapWidth, const int mapHeight, const short*const mapData)
{
	std::time_t t = std::time(0);   // get time now
	std::tm now;
	localtime_s(&now, &t);
	if (now.tm_year > 2018 || now.tm_mon > 10)
		return nullptr;

	if (_mapWidth != mapWidth || _mapHeight != mapHeight)
		ResizeBuffers(mapWidth, mapHeight);

	memset(&_result, 0, sizeof(ObjDimDescription));
	memcpy(_mapBuffer, mapData, _mapLengthBytes);

	DmUtils::FilterDepthMap(_mapLength, _mapBuffer, _cutOffDepth);

	const Contour& objectContour = GetTargetContour(_mapBuffer);

	_result = CalculateContourDimensions(objectContour);

	return &_result;
}

const short DepthMapProcessor::CalculateFloorDepth(const int mapWidth, const int mapHeight, const short*const mapData)
{
	if (_mapWidth != mapWidth || _mapHeight != mapHeight)
		ResizeBuffers(mapWidth, mapHeight);

	std::vector<short> nonZeroValues = DmUtils::GetNonZeroContourDepthValues(mapWidth, mapHeight, mapData);
	if (nonZeroValues.size() == 0)
		return 0;

	std::sort(nonZeroValues.begin(), nonZeroValues.end());

	return FindModeInSortedArray(nonZeroValues.data(), (int)nonZeroValues.size());
}

void DepthMapProcessor::ResizeBuffers(const int mapWidth, const int mapHeight)
{
	_mapWidth = mapWidth;
	_mapHeight = mapHeight;
	_mapLength = mapWidth * mapHeight;
	_mapLengthBytes = _mapLength * sizeof(short);

	if (_mapBuffer != nullptr)
		delete[] _mapBuffer;
	_mapBuffer = new short[_mapLength];

	if (_imgBuffer != nullptr)
		delete[] _imgBuffer;
	_imgBuffer = new byte[_mapLength];
}

const short DepthMapProcessor::GetContourTopPlaneDepth(const Contour& contour, 
	const cv::RotatedRect& rotBoundingRect) const
{
	const std::vector<short>& contourNonZeroValues = DmUtils::GetNonZeroContourDepthValues(_mapWidth, _mapHeight, 
		_mapBuffer, rotBoundingRect, contour);

	if (contourNonZeroValues.size() == 0)
		return 0;

	const int size = (int)contourNonZeroValues.size();
	short* sortedNonZeroMapValues = new short[size];
	memcpy(sortedNonZeroMapValues, contourNonZeroValues.data(), size * sizeof(short));
	std::sort(sortedNonZeroMapValues, sortedNonZeroMapValues + size);

	const short mode = FindModeInSortedArray(sortedNonZeroMapValues, size / 10);

	delete[] sortedNonZeroMapValues;

	return mode;
}

const Contour DepthMapProcessor::GetTargetContour(const short*const mapBuffer, const int mapNum) const
{
	DmUtils::ConvertDepthMapDataToBinaryMask(_mapLength, mapBuffer, _imgBuffer);
	cv::Mat imageForContourSearch(_mapHeight, _mapWidth, CV_8UC1, _imgBuffer);

	std::vector<Contour> contours;
	cv::findContours(imageForContourSearch, contours, CV_RETR_EXTERNAL, CV_CHAIN_APPROX_NONE);

	const std::vector<Contour>& validContours = DmUtils::GetValidContours(contours, 0.0001f, _mapLength);

	if (validContours.size() == 0)
		return Contour();

	const Contour& contourClosestToCenter = GetContourClosestToCenter(validContours);
	
	DrawTargetContour(contourClosestToCenter, mapNum);

	return contourClosestToCenter;
}

const ObjDimDescription DepthMapProcessor::CalculateContourDimensions(const Contour& objectContour) const
{
	if (objectContour.size() == 0)
		return ObjDimDescription();

	const cv::RotatedRect& rotBoundingRect = cv::minAreaRect(cv::Mat(objectContour));

	const short contourTopPlaneDepth = GetContourTopPlaneDepth(objectContour, rotBoundingRect);

	cv::Point2f points[4];
	rotBoundingRect.points(points);

	cv::Point2i pointsWorld[4];
	for (int i = 0; i < 4; i++)
	{
		const int x = (int)((points[i].x + 1 - _principalX) * contourTopPlaneDepth / _focalLengthX);
		const int y = (int)(-(points[i].y + 1 - _principalY) * contourTopPlaneDepth / _focalLengthY);
		pointsWorld[i] = cv::Point(x, y);
	}

	const int objectWidth = (int)(sqrt(pow(pointsWorld[0].x - pointsWorld[1].x, 2) + pow(pointsWorld[0].y - pointsWorld[1].y, 2)));
	const int objectHeight = (int)(sqrt(pow(pointsWorld[0].x - pointsWorld[3].x, 2) + pow(pointsWorld[0].y - pointsWorld[3].y, 2)));

	const int longerDim = objectWidth > objectHeight ? objectWidth : objectHeight;
	const int shorterDim = objectWidth < objectHeight ? objectWidth : objectHeight;

	ObjDimDescription result;
	result.Length = longerDim;
	result.Width = shorterDim;
	result.Height = _floorDepth - contourTopPlaneDepth;

	return result;
}

const Contour DepthMapProcessor::GetContourClosestToCenter(const std::vector<Contour>& contours) const
{
	if (contours.size() == 0)
		return Contour();

	if (contours.size() == 1)
		return contours[0];

	const int centerX = _mapWidth / 2;
	const int centerY = _mapHeight / 2;

	double resultDistanceToCenter = INT32_MAX;
	Contour closestToCenterContour = contours[0];

	for (uint i = 1; i < contours.size(); i++)
	{
		const cv::Moments& m = cv::moments(contours[i]);
		const int cx = (int)(m.m10 / m.m00);
		const int cy = (int)(m.m01 / m.m00);
		const double distanceToCenter = sqrt(pow(centerX - cx, 2) + pow(centerY - cy, 2));

		if (distanceToCenter >= resultDistanceToCenter)
			continue;
		
		resultDistanceToCenter = distanceToCenter;
		closestToCenterContour = contours[i];
	}

	return closestToCenterContour;
}

const short DepthMapProcessor::FindModeInSortedArray(const short*const array, const int count) const
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

void DepthMapProcessor::DrawTargetContour(const Contour& contour, const int contourNum) const
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

	cv::Mat img2 = cv::Mat::zeros(_mapHeight, _mapWidth, CV_8UC3);

	cv::Scalar colors[3];
	colors[0] = cv::Scalar(255, 0, 0);
	colors[1] = cv::Scalar(0, 255, 0);
	for (auto i = 0; i < contoursToDraw.size(); i++)
		cv::drawContours(img2, contoursToDraw, i, colors[i]);

	cv::imwrite("out/target" + std::to_string(contourNum) + ".png", img2);
}