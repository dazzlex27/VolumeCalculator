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

	_needToUpdateMeasurementVolume = false;
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

void DepthMapProcessor::SetAlgorithmSettings(const short floorDepth, const short cutOffDepth, 
	const RelPoint* polygonPoints, const int polygonPointCount, const RelRect& roiRect)
{
	_floorDepth = floorDepth;
	_cutOffDepth = cutOffDepth;
	_colorRoiRect.X = roiRect.X;
	_colorRoiRect.Y = roiRect.Y;
	_colorRoiRect.Width = roiRect.Width;
	_colorRoiRect.Height = roiRect.Height;
	_correctPerspective = true;

	_polygonPoints.clear();
	_polygonPoints.reserve(polygonPointCount);

	for (int i = 0; i < polygonPointCount; i++)
		_polygonPoints.emplace_back(cv::Point2f(polygonPoints[i].X, polygonPoints[i].Y));

	_needToUpdateMeasurementVolume = true;
}

void DepthMapProcessor::SetDebugPath(const char* path)
{
	_debugPath = std::string(path);
	_contourExtractor.SetDebugPath(_debugPath);
}

ObjDimDescription* DepthMapProcessor::CalculateObjectVolume(const DepthMap& depthMap, const bool saveDebugData)
{
	FillDepthBufferFromDepthMap(depthMap);

	DmUtils::FilterDepthMapByMaxDepth(_mapLength, _depthMapBuffer, _cutOffDepth);

	if (_needToUpdateMeasurementVolume)
	{
		UpdateMeasurementVolume(depthMap.Width, depthMap.Height);
		_needToUpdateMeasurementVolume = false;
	}

	const std::vector<DepthValue> worldDepthValues = GetWorldDepthValuesFromDepthMap();
	DmUtils::FilterDepthMapByMeasurementVolume(_depthMapBuffer, worldDepthValues, _measurementVolume);

	const Contour& objectContour = GetTargetContourFromDepthMap(saveDebugData);

	_result = CalculateContourDimensions(objectContour, saveDebugData);

	return &_result;
}

ObjDimDescription* DepthMapProcessor::CalculateObjectVolumeAlt(const DepthMap& depthMap, const ColorImage& image, const bool saveDebugData)
{
	FillDepthBufferFromDepthMap(depthMap);
	FillColorBufferFromImage(image);

	DmUtils::FilterDepthMapByMaxDepth(_mapLength, _depthMapBuffer, _cutOffDepth);

	if (_needToUpdateMeasurementVolume)
	{
		UpdateMeasurementVolume(depthMap.Width, depthMap.Height);
		_needToUpdateMeasurementVolume = false;
	}

	const std::vector<DepthValue> worldDepthValues = GetWorldDepthValuesFromDepthMap();
	DmUtils::FilterDepthMapByMeasurementVolume(_depthMapBuffer, worldDepthValues, _measurementVolume);

	const Contour& depthObjectContour = GetTargetContourFromDepthMap(saveDebugData);
	const Contour& colorObjectContour = GetTargetContourFromColorImage(saveDebugData);

	_result = CalculateContourDimensionsAlt(depthObjectContour, colorObjectContour, saveDebugData);

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

void DepthMapProcessor::FillColorBufferFromImage(const ColorImage& image)
{
	const bool dimsAreTheSame = _colorImageWidth == image.Width && _colorImageHeight == image.Height && 
		_colorImageLengthBytes == image.BytesPerPixel;
	if (!dimsAreTheSame)
	{
		_colorImageWidth = image.Width;
		_colorImageHeight = image.Height;
		const int colorImageLength = image.Width * image.Height;
		_colorImageLengthBytes = colorImageLength * image.BytesPerPixel;
		_colorImageBytesPerPixel = image.BytesPerPixel;

		if (_colorImageBuffer != nullptr)
			delete[] _colorImageBuffer;
		_colorImageBuffer = new byte[_colorImageLengthBytes];
	}

	memcpy(_colorImageBuffer, image.Data, _colorImageLengthBytes);
}

void DepthMapProcessor::FillDepthBufferFromDepthMap(const DepthMap& depthMap)
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

const Contour DepthMapProcessor::GetTargetContourFromDepthMap(const bool saveDebugData) const
{
	DmUtils::ConvertDepthMapDataToBinaryMask(_mapLength, _depthMapBuffer, _depthMaskBuffer);
	cv::Mat imageForContourSearch(_mapHeight, _mapWidth, CV_8UC1, _depthMaskBuffer);

	return _contourExtractor.ExtractContourFromBinaryImage(imageForContourSearch, saveDebugData);
}

const Contour DepthMapProcessor::GetTargetContourFromColorImage(const bool saveDebugData) const
{
	const int cvChannelsCode = DmUtils::GetCvChannelsCodeFromBytesPerPixel(_colorImageBytesPerPixel);
	cv::Mat input(_colorImageHeight, _colorImageWidth, cvChannelsCode, _colorImageBuffer);

	const cv::Rect& roi = DmUtils::GetAbsRoiFromRoiRect(_colorRoiRect, cv::Size(input.cols, input.rows));
	cv::Mat inputRoi = input(roi);

	return _contourExtractor.ExtractContourFromColorImage(inputRoi, saveDebugData);
}

const ObjDimDescription DepthMapProcessor::CalculateContourDimensions(const Contour& depthObjectContour, const bool saveDebugData) const
{
	if (depthObjectContour.size() == 0)
		return ObjDimDescription();

	const short contourTopPlaneDepth = GetContourTopPlaneDepth(depthObjectContour);

	const std::vector<DepthValue>& worldDepthValues = GetWorldDepthValues(depthObjectContour);
	const Contour& perspectiveCorrectedContour = GetCameraPoints(worldDepthValues, contourTopPlaneDepth);
	const cv::RotatedRect& correctedBoundingRect = cv::minAreaRect(perspectiveCorrectedContour);
	if (saveDebugData)
		DmUtils::DrawTargetContour(perspectiveCorrectedContour, _mapWidth, _mapHeight, _debugPath, "ctr_depth");

	const TwoDimDescription& twoDimDescription = GetTwoDimDescription(correctedBoundingRect, contourTopPlaneDepth,
		_depthIntrinsics.FocalLengthX, _depthIntrinsics.FocalLengthY,
		_depthIntrinsics.PrincipalPointX, _depthIntrinsics.PrincipalPointY);

	ObjDimDescription result;
	result.Length = twoDimDescription.Length;
	result.Width = twoDimDescription.Width;
	result.Height = _floorDepth - contourTopPlaneDepth;

	return result;
}

const ObjDimDescription DepthMapProcessor::CalculateContourDimensionsAlt(const Contour& depthObjectContour,
	const Contour& colorObjectContour, const bool saveDebugData) const
{
	if (depthObjectContour.size() == 0 || colorObjectContour.size() == 0)
		return ObjDimDescription();

	const short contourTopPlaneDepth = GetContourTopPlaneDepth(depthObjectContour);
	if (saveDebugData)
	{
		DmUtils::DrawTargetContour(depthObjectContour, _mapWidth, _mapHeight, _debugPath, "ctr_depth");
		DmUtils::DrawTargetContour(colorObjectContour, _mapWidth, _mapHeight, _debugPath, "ctr_color");
	}

	const cv::RotatedRect& colorObjectBoundingRect = cv::minAreaRect(colorObjectContour);

	const TwoDimDescription& twoDimDescription = GetTwoDimDescription(colorObjectBoundingRect, contourTopPlaneDepth,
		_colorIntrinsics.FocalLengthX, _colorIntrinsics.FocalLengthY, 
		_colorIntrinsics.PrincipalPointX, _colorIntrinsics.PrincipalPointY);

	ObjDimDescription result;
	result.Length = twoDimDescription.Length;
	result.Width = twoDimDescription.Width;
	result.Height = _floorDepth - contourTopPlaneDepth;

	return result;
}

const short DepthMapProcessor::GetContourTopPlaneDepth(const Contour& objectContour) const
{
	const cv::RotatedRect& objectBoundingRect = cv::minAreaRect(objectContour);

	const std::vector<short>& contourNonZeroValues = DmUtils::GetNonZeroContourDepthValues(_mapWidth, _mapHeight,
		_depthMapBuffer, objectBoundingRect, objectContour);

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

const std::vector<DepthValue> DepthMapProcessor::GetWorldDepthValues(const Contour& objectContour) const
{
	const double PI = 3.14159265359;

	std::vector<DepthValue> depthValues;
	depthValues.reserve(objectContour.size());

	const cv::Moments& m = cv::moments(objectContour);
	const int cx = (int)(m.m10 / m.m00);
	const int cy = (int)(m.m01 / m.m00);

	for (int i = 0; i < objectContour.size(); i++)
	{
		const int contourPointX = objectContour[i].x;
		const int contourPointY = objectContour[i].y;

		const int deltaX = contourPointX - cx;
		const int deltaY = contourPointY - cy;

		const double pointAngle = atan2(-deltaY, deltaX) * 180 / PI;

		int offsetX = 0;
		int offsetY = 0;

		if (pointAngle >= -20 && pointAngle < 20)
		{
			offsetX = -1;
			offsetY = 0;
		} 
		else if (pointAngle >= 20 && pointAngle < 70)
		{
			offsetX = -1;
			offsetY = -1;
		}
		else if (pointAngle >= 70 && pointAngle < 110)
		{
			offsetX = 0;
			offsetY = 1;
		}
		else if (pointAngle >= 110 && pointAngle <= 160)
		{
			offsetX = 1;
			offsetY = 1;
		}
		else if (pointAngle < -20 && pointAngle >= -70)
		{
			offsetX = -1;
			offsetY = 0;
		}
		else if (pointAngle < -70 && pointAngle >= 110)
		{
			offsetX = 1;
			offsetY = -1;
		}
		else if (pointAngle < 110 && pointAngle >= -160)
		{
			offsetX = 0;
			offsetY = -1;
		}
		else // [160,180] || [-160, -180]
		{
			offsetX = 1;
			offsetY = 0;
		}

		const int depthBufferIndex = contourPointY * _mapWidth + contourPointX;
		const short pointDepth = _depthMapBuffer[depthBufferIndex];

		std::vector<short> borderDepthValues;
		int tempX = contourPointX;
		int tempY = contourPointY;

		for (int j = 0; j < 5; j++)
		{
			tempX += offsetX;
			tempY += offsetY;

			const int offsetIndex = tempY * _mapWidth + tempX;
			const short offsetDepthValue = _depthMapBuffer[offsetIndex];
			if (offsetDepthValue < pointDepth)
				borderDepthValues.emplace_back(offsetDepthValue);
		}

		int elementSum = 0;
		const uint borderValuesCount = (const uint)borderDepthValues.size();
		for (uint j = 0; j < borderValuesCount; j++)
			elementSum += borderDepthValues[j];
		elementSum = borderValuesCount > 0 ? elementSum / borderValuesCount : 0;

		const short contourModeValue = elementSum > 0 ? elementSum : pointDepth;

		const int xWorld = (int)((contourPointX + 1 - _depthIntrinsics.PrincipalPointX) * contourModeValue / _depthIntrinsics.FocalLengthX);
		const int yWorld = (int)(-(contourPointY + 1 - _depthIntrinsics.PrincipalPointY) * contourModeValue / _depthIntrinsics.FocalLengthY);

		DepthValue depthValue;
		depthValue.XWorld = xWorld;
		depthValue.YWorld = yWorld;
		depthValue.Value = contourModeValue;

		depthValues.emplace_back(depthValue);
	}

	return depthValues;
}

const std::vector<cv::Point> DepthMapProcessor::GetCameraPoints(const std::vector<DepthValue>& depthValues, const short targetDepth) const
{
	std::vector<cv::Point> cameraPoints;
	cameraPoints.reserve(depthValues.size());

	for (int i = 0; i < depthValues.size(); i++)
	{
		const int xWorld = depthValues[i].XWorld;
		const int yWorld = depthValues[i].YWorld;

		const int contourPointX = (int)(xWorld * _depthIntrinsics.FocalLengthX / targetDepth + _depthIntrinsics.PrincipalPointX - 1);
		const int contourPointY = (int)(-(yWorld * _depthIntrinsics.FocalLengthY / targetDepth) + _depthIntrinsics.PrincipalPointY - 1);

		cv::Point cameraPoint;
		cameraPoint.x = contourPointX;
		cameraPoint.y = contourPointY;
		cameraPoints.emplace_back(cameraPoint);
	}

	return cameraPoints;
}

const bool DepthMapProcessor::IsObjectInZone(const std::vector<DepthValue>& contour) const
{
	for (int i = 0; i < contour.size(); i++)
	{
		if (DmUtils::IsPointInZone(contour[i], _measurementVolume))
			return true;
	}

	return false;
}

void DepthMapProcessor::UpdateMeasurementVolume(const int mapWidth, const int mapHeight)
{
	_measurementVolume.largerDepthValue = _floorDepth;
	_measurementVolume.smallerDepthValue = 600;

	_measurementVolume.Points.clear();
	_measurementVolume.Points.reserve(_polygonPoints.size());

	for (int i = 0; i < _polygonPoints.size(); i++)
	{
		cv::Point point((int)(_polygonPoints[i].x * mapWidth), (int)(_polygonPoints[i].y * mapHeight));
		const int x0World = (int)((point.x + 1 - _depthIntrinsics.PrincipalPointX) * (_floorDepth + 50) / _depthIntrinsics.FocalLengthX);
		const int y0World = (int)(-(point.y + 1 - _depthIntrinsics.PrincipalPointY) * (_floorDepth + 50) / _depthIntrinsics.FocalLengthY);
		_measurementVolume.Points.emplace_back(cv::Point(x0World, y0World));
	}

	const int width = abs(_measurementVolume.Points[3].x - _measurementVolume.Points[0].x);
	const int height = abs(_measurementVolume.Points[0].y - _measurementVolume.Points[1].y);
}

const std::vector<DepthValue> DepthMapProcessor::GetWorldDepthValuesFromDepthMap()
{
	std::vector<DepthValue> depthValues;

	for (int j = 0; j < _mapHeight; j++)
	{
		for (int i = 0; i < _mapWidth; i++)
		{
			const short depth = _depthMapBuffer[j*_mapWidth + i];
			const int xWorld = (int)((i + 1 - _depthIntrinsics.PrincipalPointX) * depth / _depthIntrinsics.FocalLengthX);
			const int yWorld = (int)(-(j + 1 - _depthIntrinsics.PrincipalPointY) * depth / _depthIntrinsics.FocalLengthY);

			DepthValue depthValue;
			depthValue.Value = depth;
			depthValue.XWorld = xWorld;
			depthValue.YWorld = yWorld;
			depthValues.emplace_back(depthValue);
		}
	}

	return depthValues;
}