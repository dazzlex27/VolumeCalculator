#include "VolumeChecker.h"
#include <string>
#include <numeric>
#include <cmath>
#include "DmUtils.h"

const double PI = 3.141592653589793238463;

VolumeChecker::VolumeChecker(const float fovX, const float fovY, const int mapWidth, const int mapHeight, const int cutOffDepth)
	: _halfFovX(fovX * 0.5 / 180.0 * PI),
	_halfFovY(fovY * 0.5 / 180.0 * PI),
	_mapWidth(mapWidth),
	_mapHeight(mapHeight),
	_mapLength(mapWidth * mapHeight),
	_mapLengthBytes(sizeof(short) * _mapLength),
	_cutOffDepth(cutOffDepth)
{
	_mapBuffer = new short[_mapLength];
	memset(_mapBuffer, 0, _mapLengthBytes);

	_imgBuffer = new byte[_mapLength];
	memset(_imgBuffer, 0, _mapLength);

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

ObjDimDescription* VolumeChecker::GetVolume(const short*const mapData)
{
	memset(_result, 0, sizeof(ObjDimDescription));
	memcpy(_mapBuffer, mapData, _mapLengthBytes);

	DmUtils::FilterDepthMap(_mapLength, _mapBuffer, _cutOffDepth);

	const Contour& largestContour = GetLargestContour();

	const AbsRect& rect = DmUtils::CalculateContourBoundingBox(largestContour);
	const cv::Rect contourRect(rect.X + 1, rect.Y + 1, rect.Width - 1, rect.Height - 1);

	const std::vector<short>& contourValues = DmUtils::GetContourDepthValues(_mapWidth, _mapBuffer, contourRect);
	const short avgValue = GetAverageAreaValue(contourValues);

	const float relObjX = (float)contourRect.x / _mapWidth;
	const float relObjY = (float)contourRect.y / _mapHeight;
	const float relObjWidth = (float)contourRect.width / _mapWidth;
	const float relObjHeight = (float)contourRect.height / _mapHeight;

	const RelRect objRectRel = { relObjX, relObjY, relObjWidth, relObjHeight };

	const AbsRect planeSizeAtObjHeightMm = CalculatePlaneSizeAtGivenHeight(avgValue);

	_result->Width = (short)(planeSizeAtObjHeightMm.Width * objRectRel.Width);
	_result->Height = _cutOffDepth - avgValue;
	_result->Depth = (short)(planeSizeAtObjHeightMm.Height * objRectRel.Height);

	return _result;
}

const short VolumeChecker::GetAverageAreaValue(const std::vector<short>& values)
{
	// TODO: remove overflow possibility
	 double sum_of_elems = std::accumulate(values.begin(), values.end(), 0.0);

	 return (short)(sum_of_elems / values.size());
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

	return largestContour;
}