#include "DepthMapProcessor.h"
#include <string>
#include <cmath>
#include <climits>
#include <ctime>
#include "ProcessingUtils.h"

DepthMapProcessor::DepthMapProcessor(ColorCameraIntristics colorIntrinsics, DepthCameraIntristics depthIntrinsics)
	: _colorIntrinsics(colorIntrinsics), _depthIntrinsics(depthIntrinsics)
{
	_mapWidth = 0;
	_mapHeight = 0;
	_mapLength = 0;
	_mapLengthBytes = 0;

	_depthMapBuffer = nullptr;
	_depthMaskBuffer = nullptr;
	_colorImageBuffer = nullptr;
}

DepthMapProcessor::~DepthMapProcessor()
{
	if (_depthMapBuffer != nullptr)
	{
		delete[] _depthMapBuffer;
		_depthMapBuffer = nullptr;
	}

	if (_depthMaskBuffer != nullptr)
	{
		delete[] _depthMaskBuffer;
		_depthMaskBuffer = nullptr;
	}

	if (_colorImageBuffer != nullptr)
	{
		delete[] _colorImageBuffer;
		_colorImageBuffer = nullptr;
	}
}

void DepthMapProcessor::SetAlgorithmSettings(const short floorDepth, const short cutOffDepth, const RelRect& roiRect)
{
	_floorDepth = floorDepth;
	_cutOffDepth = cutOffDepth;
	_colorRoiRect.X = roiRect.X;
	_colorRoiRect.Y = roiRect.Y;
	_colorRoiRect.Width = roiRect.Width;
	_colorRoiRect.Height = roiRect.Height;
	_correctPerspective = true;
}

ObjDimDescription* DepthMapProcessor::CalculateObjectVolume(const DepthMap& depthMap)
{
	FillDepthBuffer(depthMap);
	DmUtils::FilterDepthMap(_mapLength, _depthMapBuffer, _cutOffDepth);
	 
	const Contour& objectContour = GetTargetContourFromDepthMap();

	_result = CalculateContourDimensions(objectContour);

	return &_result;
}

ObjDimDescription* DepthMapProcessor::CalculateObjectVolumeAlt(const DepthMap& depthMap, const ColorImage& image)
{
	FillDepthBuffer(depthMap);
	DmUtils::FilterDepthMap(_mapLength, _depthMapBuffer, _cutOffDepth);
	FillColorBuffer(image);

	const Contour& depthObjectContour = GetTargetContourFromDepthMap();
	const Contour& colorObjectContour = GetTargetContourFromColorFrame();

	_result = CalculateContourDimensionsAlt(depthObjectContour, colorObjectContour);

	return &_result;
}

const short DepthMapProcessor::CalculateFloorDepth(const DepthMap& depthMap)
{
	std::vector<short> nonZeroValues = DmUtils::GetNonZeroContourDepthValues(depthMap);
	if (nonZeroValues.size() == 0)
		return 0;

	std::sort(nonZeroValues.begin(), nonZeroValues.end());

	return DmUtils::FindModeInSortedArray(nonZeroValues.data(), (int)nonZeroValues.size());
}

void DepthMapProcessor::FillColorBuffer(const ColorImage& image)
{
	const bool dimsAreTheSame = _colorImageWidth == image.Width && _colorImageHeight == image.Height && 
		_colorImageLengthBytes == image.BytesPerPixel;
	if (!dimsAreTheSame)
	{
		_colorImageWidth = image.Width;
		_colorImageHeight = image.Height;
		_colorImageLength = image.Width * image.Height;
		_colorImageLengthBytes = _colorImageLength * image.BytesPerPixel;
		_colorImageBytesPerPixel = image.BytesPerPixel;

		if (_colorImageBuffer != nullptr)
			delete[] _colorImageBuffer;
		_colorImageBuffer = new byte[_colorImageLengthBytes];
	}

	memcpy(_colorImageBuffer, image.Data, _colorImageLengthBytes);
}

void DepthMapProcessor::FillDepthBuffer(const DepthMap& depthMap)
{
	const bool dimsAreTheSame = _mapWidth == depthMap.Width && _mapHeight == depthMap.Height;
	if (!dimsAreTheSame)
	{
		_mapWidth = depthMap.Width;
		_mapHeight = depthMap.Height;
		_mapLength = _mapWidth * _mapHeight;
		_mapLengthBytes = _mapLength * sizeof(short);

		if (_depthMapBuffer != nullptr)
			delete[] _depthMapBuffer;
		_depthMapBuffer = new short[_mapLength];

		if (_depthMaskBuffer != nullptr)
			delete[] _depthMaskBuffer;
		_depthMaskBuffer = new byte[_mapLength];
	}

	memset(&_result, 0, sizeof(ObjDimDescription));
	memcpy(_depthMapBuffer, depthMap.Data, _mapLengthBytes);
}

const Contour DepthMapProcessor::GetTargetContourFromDepthMap() const
{
	DmUtils::ConvertDepthMapDataToBinaryMask(_mapLength, _depthMapBuffer, _depthMaskBuffer);
	cv::Mat imageForContourSearch(_mapHeight, _mapWidth, CV_8UC1, _depthMaskBuffer);

	std::vector<Contour> contours;
	cv::findContours(imageForContourSearch, contours, CV_RETR_EXTERNAL, CV_CHAIN_APPROX_NONE);

	const std::vector<Contour>& validContours = DmUtils::GetValidContours(contours, 0.0001f, _mapLength);

	if (validContours.size() == 0)
		return Contour();

	const Contour& contourClosestToCenter = GetContourClosestToCenter(validContours);
	
	DmUtils::DrawTargetContour(contourClosestToCenter, _mapWidth, _mapHeight, 0);

	return contourClosestToCenter;
}

const Contour DepthMapProcessor::GetTargetContourFromColorFrame() const
{
	const int cvChannelsCode = DmUtils::GetCvChannelsCodeFromBytesPerPixel(_colorImageBytesPerPixel);
	cv::Mat input(_colorImageHeight, _colorImageWidth, cvChannelsCode, _colorImageBuffer);

	const cv::Rect& roi = DmUtils::GetAbsRoiFromRoiRect(_colorRoiRect, cv::Size(input.cols, input.rows));
	cv::Mat inputRoi = input(roi);
	cv::imwrite("out/inputRoi.png", inputRoi);

	cv::Mat cannied;
	cv::Canny(inputRoi, cannied, _cannyThreshold1, _cannyThreshold2);
	cv::imwrite("out/cannied.png", cannied);

	std::vector<Contour> contours;
	cv::findContours(cannied, contours, CV_RETR_EXTERNAL, CV_CHAIN_APPROX_NONE);

	Contour mergedContour;
	for (int i = 0; i < contours.size(); i++)
	{
		for (int j = 0; j < contours[i].size(); j++)
			mergedContour.emplace_back(contours[i][j]);
	}

	DmUtils::DrawTargetContour(mergedContour, _colorImageWidth, _colorImageHeight, 1);

	return mergedContour;
}

const ObjDimDescription DepthMapProcessor::CalculateContourDimensions(const Contour& depthObjectContour) const
{
	if (depthObjectContour.size() == 0)
		return ObjDimDescription();

	const cv::RotatedRect& depthObjectBoundingRect = cv::minAreaRect(cv::Mat(depthObjectContour));
	const short contourTopPlaneDepth = GetContourTopPlaneDepth(depthObjectContour, depthObjectBoundingRect);

	const TwoDimDescription& twoDimDescription = GetTwoDimDescription(depthObjectBoundingRect, contourTopPlaneDepth,
		_depthIntrinsics.FocalLengthX, _depthIntrinsics.FocalLengthY,
		_depthIntrinsics.PrincipalPointX, _depthIntrinsics.PrincipalPointY);

	ObjDimDescription result;
	result.Length = twoDimDescription.Length;
	result.Width = twoDimDescription.Width;
	result.Height = _floorDepth - contourTopPlaneDepth;

	return result;
}

const ObjDimDescription DepthMapProcessor::CalculateContourDimensionsAlt(const Contour& depthObjectContour,
	const Contour& colorObjectContour) const
{
	if (depthObjectContour.size() == 0 || colorObjectContour.size() == 0)
		return ObjDimDescription();

	const cv::RotatedRect& depthObjectBoundingRect = cv::minAreaRect(cv::Mat(depthObjectContour));
	const short contourTopPlaneDepth = GetContourTopPlaneDepth(depthObjectContour, depthObjectBoundingRect);

	const cv::RotatedRect& colorObjectBoundingRect = cv::minAreaRect(cv::Mat(colorObjectContour));

	const TwoDimDescription& twoDimDescription = GetTwoDimDescription(colorObjectBoundingRect, contourTopPlaneDepth,
		_colorIntrinsics.FocalLengthX, _colorIntrinsics.FocalLengthY, 
		_colorIntrinsics.PrincipalPointX, _colorIntrinsics.PrincipalPointY);

	ObjDimDescription result;
	result.Length = twoDimDescription.Length;
	result.Width = twoDimDescription.Width;
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

const short DepthMapProcessor::GetContourTopPlaneDepth(const Contour& contour, const cv::RotatedRect& rotBoundingRect) const
{
	const std::vector<short>& contourNonZeroValues = DmUtils::GetNonZeroContourDepthValues(_mapWidth, _mapHeight,
		_depthMapBuffer, rotBoundingRect, contour);

	if (contourNonZeroValues.size() == 0)
		return 0;

	const int size = (int)contourNonZeroValues.size();
	short* sortedNonZeroMapValues = new short[size];
	memcpy(sortedNonZeroMapValues, contourNonZeroValues.data(), size * sizeof(short));
	std::sort(sortedNonZeroMapValues, sortedNonZeroMapValues + size);

	const short mode = DmUtils::FindModeInSortedArray(sortedNonZeroMapValues, size / 20);

	delete[] sortedNonZeroMapValues;

	return mode;
}

const TwoDimDescription DepthMapProcessor::GetTwoDimDescription(const cv::RotatedRect& contourBoundingRect, 
	const short contourTopPlaneDepth, const float fx, const float fy, const float ppx, const float ppy) const
{
	cv::Point2f points[4];
	contourBoundingRect.points(points);

	cv::Point2i pointsWorld[4];
	for (int i = 0; i < 4; i++)
	{
		const int xWorld = (int)((points[i].x + 1 - ppx) * contourTopPlaneDepth / fx);
		const int yWorld = (int)(-(points[i].y + 1 - ppy) * contourTopPlaneDepth / fy);
		pointsWorld[i] = cv::Point(xWorld, yWorld);
	}

	const int objectWidth = (int)DmUtils::GetDistanceBetweenPoints(pointsWorld[0].x, pointsWorld[0].y, pointsWorld[1].x, pointsWorld[1].y);
	const int objectHeight = (int)DmUtils::GetDistanceBetweenPoints(pointsWorld[0].x, pointsWorld[0].y, pointsWorld[3].x, pointsWorld[3].y);

	TwoDimDescription twoDimDescription;
	twoDimDescription.Length = objectWidth > objectHeight ? objectWidth : objectHeight;
	twoDimDescription.Width = objectWidth < objectHeight ? objectWidth : objectHeight;

	return twoDimDescription;
}