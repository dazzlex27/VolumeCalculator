#include "VolumeChecker.h"
#include <string>
#include <numeric>
#include <cmath>
#include "DmUtils.h"
#include <climits>

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

ObjDimDescription* VolumeChecker::GetVolume(const int mapWidth, const int mapHeight, const short*const mapData)
{
	if (_mapWidth != mapWidth || _mapHeight != mapHeight)
		ResizeBuffers(mapWidth, mapHeight);

	memset(_result, 0, sizeof(ObjDimDescription));
	memcpy(_mapBuffer, mapData, _mapLengthBytes);

	DmUtils::FilterDepthMap(_mapLength, _mapBuffer, _cutOffDepth);

	const Contour& largestContour = GetLargestContour();

	cv::RotatedRect rotBoundingRect = cv::minAreaRect(cv::Mat(largestContour));

	float angle = rotBoundingRect.angle;
	//if (angle < 0)
	//	angle = 90 + rotBoundingRect.angle;

	const std::vector<short>& contourValues = DmUtils::GetContourDepthValues(_mapWidth, _mapHeight, _mapBuffer, rotBoundingRect, 
		largestContour);

	const int mapWidthAngled = _mapWidth / (cos(angle / 180 * PI));
	const int mapHeightAngled = _mapHeight / (cos(angle / 180 * PI));

	const RotRelRect& contourRelRect = DmUtils::RotAbsRectToRel(mapWidthAngled, mapHeightAngled, rotBoundingRect);

	const short contourClosestPoint = GetAverageAreaValue(contourValues);
	const AbsRect& planeSizeAtObjHeightMm = CalculatePlaneSizeAtGivenHeight(contourClosestPoint);

	_result->Width = (short)(planeSizeAtObjHeightMm.Width * contourRelRect.Width);
	_result->Height = _floorDepth - contourClosestPoint;
	_result->Depth = (short)(planeSizeAtObjHeightMm.Height * contourRelRect.Height);

	return _result;
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

	if (_imgBuffer != nullptr)
		delete[] _imgBuffer;

	_imgBuffer = new byte[_mapLength];
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

const Contour VolumeChecker::GetLargestContour()
{
	DmUtils::ConvertDepthMapDataToBinaryMask(_mapLength, _mapBuffer, _imgBuffer);
	cv::Mat img(_mapHeight, _mapWidth, CV_8UC1, _imgBuffer);

	cv::imwrite("out/input.png", img);

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
	
	cv::imwrite("out/object.png", img2);

	return largestContour;
}