#include "VolumeChecker.h"
#include <string>
#include <numeric>
#include <cmath>
#include "DmUtils.h"
#include <climits>
#include <map>

const double PI = 3.141592653589793238463;

VolumeChecker::VolumeChecker(const float fovX, const float fovY)
	: _halfFovX(fovX * 0.5 / 180.0 * PI),
	_halfFovY(fovY * 0.5 / 180.0 * PI)
{
	_mapWidth = 0;
	_mapHeight = 0;
	_mapLength = 0;
	_mapLengthBytes = 0;

	_result = new ObjDimDescription();
	memset(_result, 0, sizeof(ObjDimDescription));
}

VolumeChecker::~VolumeChecker()
{
	delete[] _mapBuffer;
	_mapBuffer = 0;

	delete[] _imgBuffer;
	_imgBuffer = 0;

	delete _result;
	_result = 0;
}

void VolumeChecker::SetSettings(const short minDepth, const short floorDepth, const short cutOffDepth)
{
	_minDepth = minDepth;
	_floorDepth = floorDepth;
	_cutOffDepth = cutOffDepth;
}

ObjDimDescription* VolumeChecker::CalculateVolume(const int mapWidth, const int mapHeight, const short*const mapData)
{
	if (_mapWidth != mapWidth || _mapHeight != mapHeight)
		ResizeBuffers(mapWidth, mapHeight);

	memset(_result, 0, sizeof(ObjDimDescription));
	memcpy(_mapBuffer, mapData, _mapLengthBytes);

	DmUtils::FilterDepthMap(_mapLength, _mapBuffer, _cutOffDepth);

	const Contour& largestContour = GetLargestContour(_mapBuffer);

	const cv::RotatedRect& rotBoundingRect = cv::minAreaRect(cv::Mat(largestContour));

	const float angleCos = cos(rotBoundingRect.angle / 180 * PI);

	const int mapWidthAngled = _mapWidth / angleCos;
	const int mapHeightAngled = _mapHeight / angleCos;

	const RotRelRect& contourRelRect = DmUtils::RotAbsRectToRel(mapWidthAngled, mapHeightAngled, rotBoundingRect);

	const std::vector<short>& contourValues = DmUtils::GetContourDepthValues(_mapWidth, _mapHeight, _mapBuffer, 
		rotBoundingRect, largestContour);

	const short contourClosestPoint = GetAverageAreaValue(contourValues);
	const AbsRect& planeSizeAtObjHeightMm = CalculatePlaneSizeAtGivenHeight(contourClosestPoint);

	_result->Width = (short)(planeSizeAtObjHeightMm.Width / angleCos * contourRelRect.Width);
	_result->Height = _floorDepth - contourClosestPoint;
	_result->Depth = (short)(planeSizeAtObjHeightMm.Height / angleCos * contourRelRect.Height);

	return _result;
}

ObjDimDescription* VolumeChecker::GetVolumeFromStereo(const int mapWidth, const int mapHeight, const short*const mapData1, 
	const short*const mapData2, const int offsetXmm, const int offsetYmm)
{
	if (_mapWidth != mapWidth || _mapHeight != mapHeight)
		ResizeBuffers(mapWidth, mapHeight);

	memset(_result, 0, sizeof(ObjDimDescription));
	memcpy(_mapBuffer, mapData1, _mapLengthBytes);
	memcpy(_mapBuffer2, mapData2, _mapLengthBytes);

	DmUtils::FilterDepthMap(_mapLength, _mapBuffer, _cutOffDepth);
	DmUtils::FilterDepthMap(_mapLength, _mapBuffer2, _cutOffDepth);

	const Contour& largestContour1 = GetLargestContour(_mapBuffer);
	const Contour& largestContour2 = GetLargestContour(_mapBuffer2);

	const cv::RotatedRect& rotBoundingRect1 = cv::minAreaRect(cv::Mat(largestContour1));
	const cv::RotatedRect& rotBoundingRect2 = cv::minAreaRect(cv::Mat(largestContour2));
	
	const float angleCos = cos(rotBoundingRect1.angle / 180 * PI);
	const int mapWidthAngled = _mapWidth / angleCos;
	const int mapHeightAngled = _mapHeight / angleCos;

	const std::vector<short>& contourValues1 = DmUtils::GetContourDepthValues(_mapWidth, _mapHeight, _mapBuffer,
		rotBoundingRect1, largestContour1);
	const short contourClosestPoint1 = GetAverageAreaValue(contourValues1);
	const AbsRect& planeSizeAtObjHeightMm1 = CalculatePlaneSizeAtGivenHeight(contourClosestPoint1);

	const std::vector<short>& contourValues2 = DmUtils::GetContourDepthValues(_mapWidth, _mapHeight, _mapBuffer2, 
		rotBoundingRect2, largestContour2);
	const short contourClosestPoint2 = GetAverageAreaValue(contourValues2);
	const AbsRect& planeSizeAtObjHeightMm2 = CalculatePlaneSizeAtGivenHeight(contourClosestPoint2);

	const cv::RotatedRect& mappedSecondContourRect = MapSecondContourBoxToFirstImage(rotBoundingRect2, planeSizeAtObjHeightMm2, offsetXmm, offsetYmm);

	Contour mergedContour;
	//mergedContour.emplace_back(rotBoundingRect1.points[0]);
	//mergedContour.emplace_back(rotBoundingRect1.points[1]);
	//mergedContour.emplace_back(rotBoundingRect1.points[2]);
	//mergedContour.emplace_back(rotBoundingRect1.points[3]);
	//mergedContour.emplace_back(rotBoundingRect2.points[0]);
	//mergedContour.emplace_back(rotBoundingRect2.points[1]);
	//mergedContour.emplace_back(rotBoundingRect2.points[2]);
	//mergedContour.emplace_back(rotBoundingRect2.points[3]);

	const cv::RotatedRect& totalContour = cv::minAreaRect(cv::Mat(mergedContour));
	const RotRelRect& totaContourRelRect = DmUtils::RotAbsRectToRel(mapWidthAngled, mapHeightAngled, totalContour);

	_result->Width = (short)(planeSizeAtObjHeightMm1.Width / angleCos * totaContourRelRect.Width);
	_result->Height = _floorDepth - contourClosestPoint1;
	_result->Depth = (short)(planeSizeAtObjHeightMm1.Height / angleCos * totaContourRelRect.Height);

	return _result;
}

short VolumeChecker::CalculateFloorDepth(const int mapWidth, const int mapHeight, const short*const mapData)
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

void VolumeChecker::ResizeBuffers(const int mapWidth, const int mapHeight)
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

const cv::RotatedRect VolumeChecker::MapSecondContourBoxToFirstImage(const cv::RotatedRect& rect, const AbsRect& planeRect,
	const int offsetXmm, const int offsetYmm)
{
	cv::RotatedRect result;



	return result;
}

const short VolumeChecker::GetAverageAreaValue(const std::vector<short>& values)
{
	const int size = (int)values.size();
	short* tempArr = new short[size];
	memcpy(tempArr, values.data(), size * sizeof(short));

	std::sort(tempArr, tempArr + size);
	
	const int top10PercentIndex = 0;
	short number = INT16_MAX;
	short mode = number;
	int count = 1;
	int countMode = 1;

	for (int i = top10PercentIndex + 1; i < size / 10; i++)
	{
		if (tempArr[i] == number)
		{
			count++;
		}
		else
		{
			if (count > countMode)
			{
				countMode = count;
				mode = number;
			}
			count = 1;
			number = tempArr[i];
		}
	}


	 return (short)mode;
}

const AbsRect VolumeChecker::CalculatePlaneSizeAtGivenHeight(const short height)
{
	const float tnX = tan(_halfFovX);
	const float tnY = tan(_halfFovY);

	const int horizontalPlaneLength = (int)(height * tnX * 2);
	const int verticalPlaneLength = (int)(height * tnY * 2);

	return AbsRect{ 0, 0, horizontalPlaneLength, verticalPlaneLength };
}

const Contour VolumeChecker::GetLargestContour(const short*const mapBuffer, const int mapNum)
{
	DmUtils::ConvertDepthMapDataToBinaryMask(_mapLength, mapBuffer, _imgBuffer);
	cv::Mat img(_mapHeight, _mapWidth, CV_8UC1, _imgBuffer);

	cv::imwrite("out/input" + std::to_string(mapNum) + ".png", img);

	std::vector<Contour> contours;
	cv::findContours(img, contours, CV_RETR_EXTERNAL, CV_CHAIN_APPROX_NONE);

	if (contours.size() == 0)
		return Contour();

	Contour largestContour = contours[0];
	auto largestSquare = contours[0].size();

	for (uint i = 1; i < contours.size(); i++)
	{
		auto currentSquare = contours[i].size();
		if (currentSquare > largestSquare)
		{
			largestContour = contours[i];
			largestSquare = currentSquare;
		}
	}
	
	cv::RotatedRect rect = cv::minAreaRect(cv::Mat(largestContour));
	cv::Point2f points[4];
	rect.points(points);

	Contour rectContour;
	rectContour.emplace_back(points[0]);
	rectContour.emplace_back(points[1]);
	rectContour.emplace_back(points[2]);
	rectContour.emplace_back(points[3]);

	std::vector<Contour> contoursToDraw;
	contoursToDraw.emplace_back(largestContour);
	contoursToDraw.emplace_back(rectContour);

	cv::Mat img2 = cv::Mat::zeros(_mapHeight, _mapWidth, CV_8UC3);

	cv::Scalar colors[3];
	colors[0] = cv::Scalar(255, 0, 0);
	colors[1] = cv::Scalar(0, 255, 0);
	for (size_t idx = 0; idx < contoursToDraw.size(); idx++) 
		cv::drawContours(img2, contoursToDraw, idx, colors[idx % 2]);
	
	cv::imwrite("out/output" + std::to_string(mapNum) + ".png", img2);

	return largestContour;
}