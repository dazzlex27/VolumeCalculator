#include "DepthMapProcessor.h"
#include <string>
#include <numeric>
#include <cmath>
#include <climits>
#include "ProcessingUtils.h"

const float PI = 3.141592653589793238463f;

DepthMapProcessor::DepthMapProcessor(const float fovX, const float fovY)
	: _halfFovX(fovX * 0.5f / 180.0f * PI),
	_halfFovY(fovY * 0.5f / 180.0f * PI)
{
	_mapWidth = 0;
	_mapHeight = 0;
	_mapLength = 0;
	_mapLengthBytes = 0;

	_result = new ObjDimDescription();
	memset(_result, 0, sizeof(ObjDimDescription));
}

DepthMapProcessor::~DepthMapProcessor()
{
	delete[] _mapBuffer;
	_mapBuffer = 0;

	delete[] _imgBuffer;
	_imgBuffer = 0;

	delete _result;
	_result = 0;
}

void DepthMapProcessor::SetSettings(const short minDepth, const short floorDepth, const short cutOffDepth)
{
	_minDepth = minDepth;
	_floorDepth = floorDepth;
	_cutOffDepth = cutOffDepth;
}

#include <ctime>

ObjDimDescription* DepthMapProcessor::CalculateObjectVolume(const int mapWidth, const int mapHeight, const short*const mapData)
{
	std::time_t t = std::time(0);   // get time now
	std::tm now;
	localtime_s(&now, &t);
	if (now.tm_year > 2018 || now.tm_mon > 10)
		return nullptr;

	if (_mapWidth != mapWidth || _mapHeight != mapHeight)
		ResizeBuffers(mapWidth, mapHeight);

	memset(_result, 0, sizeof(ObjDimDescription));
	memcpy(_mapBuffer, mapData, _mapLengthBytes);

	DmUtils::FilterDepthMap(_mapLength, _mapBuffer, _cutOffDepth);

	const Contour& objectContour = GetTargetContour(_mapBuffer);

	const cv::RotatedRect& rotBoundingRect = cv::minAreaRect(cv::Mat(objectContour));

	const float angleCos = cos(rotBoundingRect.angle / 180 * PI);

	const int mapWidthAngled = (int)(_mapWidth / angleCos);
	const int mapHeightAngled = (int)(_mapHeight / angleCos);

	const RotRelRect& contourRelRect = DmUtils::RotAbsRectToRel(mapWidthAngled, mapHeightAngled, rotBoundingRect);

	const short contourTopPlaneDepth = GetContourTopPlaneDepth(objectContour, rotBoundingRect);
	const AbsRect& planeSizeAtObjHeightMm = CalculatePlaneSizeAtGivenHeight(contourTopPlaneDepth);

	_result->Width = (short)(planeSizeAtObjHeightMm.Width / angleCos * contourRelRect.Width);
	_result->Height = _floorDepth - contourTopPlaneDepth;
	_result->Depth = (short)(planeSizeAtObjHeightMm.Height / angleCos * contourRelRect.Height);

	return _result;
}

short DepthMapProcessor::CalculateFloorDepth(const int mapWidth, const int mapHeight, const short*const mapData)
{
	if (_mapWidth != mapWidth || _mapHeight != mapHeight)
		ResizeBuffers(mapWidth, mapHeight);

	memcpy(_mapBuffer, mapData, _mapLengthBytes);

	std::map<short, int> mapValues;

	for (int i = 0; i < _mapLength; i++)
	{
		const short value = _mapBuffer[i];

		const std::map<short, int>::iterator& it = mapValues.find(value);

		if (it == mapValues.end())
			mapValues[value] = 0;
		else
			mapValues[value]++;
	}

	int maxOccurence = -1;
	auto maxOccurenceIterator = mapValues.begin();

	for (auto it = mapValues.begin(); it != mapValues.end(); it++)
	{
		if (it->second > maxOccurence)
		{
			maxOccurence = it->second;
			maxOccurenceIterator = it;
		}
	}

	return maxOccurenceIterator->first;
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

	if (_mapBuffer2 != nullptr)
		delete[] _mapBuffer2;
	_mapBuffer2 = new short[_mapLength];

	if (_imgBuffer != nullptr)
		delete[] _imgBuffer;
	_imgBuffer = new byte[_mapLength];
}

const short DepthMapProcessor::GetContourTopPlaneDepth(const Contour& contour, 
	const cv::RotatedRect& rotBoundingRect) const
{
	const std::vector<short>& contourNonZeroValues = DmUtils::GetNonZeroContourDepthValues(_mapWidth, _mapHeight, 
		_mapBuffer, rotBoundingRect, contour);

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

const Contour DepthMapProcessor::GetContourClosestToCenter(const std::vector<Contour>& contours) const
{
	const int centerX = _mapWidth / 2;
	const int centerY = _mapHeight / 2;

	double resultDistanceToCenter = INT32_MAX;
	Contour closestToCenterContour;

	for (uint i = 1; i < contours.size(); i++)
	{
		const cv::Moments& m = cv::moments(contours[i]);
		const int cx = (int)(m.m10 / m.m00);
		const int cy = (int)(m.m01 / m.m00);
		const double distanceToCenter = sqrt(pow(centerX - cx, 2) + pow(centerY - cy, 2));

		if (distanceToCenter < resultDistanceToCenter)
		{
			resultDistanceToCenter = distanceToCenter;
			closestToCenterContour = contours[i];
		}
	}

	return closestToCenterContour;
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


const AbsRect DepthMapProcessor::CalculatePlaneSizeAtGivenHeight(const short height) const
{
	const float tnX = tan(_halfFovX);
	const float tnY = tan(_halfFovY);

	const int horizontalPlaneLength = (int)(height * tnX * 2);
	const int verticalPlaneLength = (int)(height * tnY * 2);

	return AbsRect{ 0, 0, horizontalPlaneLength, verticalPlaneLength };
}

const short DepthMapProcessor::FindModeInSortedArray(const short*const array, const int count) const
{
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