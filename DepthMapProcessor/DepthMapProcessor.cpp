#include "DepthMapProcessor.h"
#include <string>
#include <cmath>
#include <climits>
#include <cstdlib>
#include <ctime>
#include "DmUtils.h"

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

const int DepthMapProcessor::SelectAlgorithm(const DepthMap& depthMap, const ColorImage& colorImage, const long measuredDistance,
	const bool dm1Enabled, const bool dm2Enabled, const bool rgbEnabled)
{
	const bool dataIsValid = depthMap.Data != nullptr && colorImage.Data != nullptr;
	if (!dataIsValid)
		return -2;

	const bool atLeastOneModeIsEnabled = dm1Enabled || dm2Enabled || rgbEnabled;
	if (!atLeastOneModeIsEnabled)
		return -3;

	const bool onlyDm1IsEnabled = dm1Enabled && !dm2Enabled && !rgbEnabled;
	if (onlyDm1IsEnabled)
		return 0;

	const bool onlyDm2IsEnabled = !dm1Enabled && dm2Enabled && !rgbEnabled;
	if (onlyDm2IsEnabled)
		return 1;

	const bool onlyRgbIsEnabled = !dm1Enabled && !dm2Enabled && rgbEnabled;
	if (onlyRgbIsEnabled)
		return 2;

	FillDepthBufferFromDepthMap(depthMap);
	FillColorBufferFromImage(colorImage);

	DmUtils::FilterDepthMapByMaxDepth(_mapLength, _depthMapBuffer, _cutOffDepth);

	if (_needToUpdateMeasurementVolume)
	{
		UpdateMeasurementVolume(depthMap.Width, depthMap.Height);
		_needToUpdateMeasurementVolume = false;
	}

	const std::vector<DepthValue> worldDepthValues = GetWorldDepthValuesFromDepthMap();
	DmUtils::FilterDepthMapByMeasurementVolume(_depthMapBuffer, worldDepthValues, _measurementVolume);

	const Contour& depthObjectContour = GetTargetContourFromDepthMap(false);
	const Contour& colorObjectContour = rgbEnabled ? GetTargetContourFromColorImage(false) : Contour();

	const bool colorContourIsEmpty = colorObjectContour.size() == 0;
	const bool depthContourIsEmpty = depthObjectContour.size() == 0;

	const int depthContourArea = depthContourIsEmpty ? 0 : (int)cv::contourArea(depthObjectContour);
	const bool noDepthObject = depthContourArea < 4;
	const bool bothContoursAreEmpty = noDepthObject && colorContourIsEmpty;
	if (bothContoursAreEmpty)
		return -1;

	const ContourPlanes& contourPlanes = GetDepthContourPlanes(depthObjectContour);
	const short measuredDistanceShort = measuredDistance > SHRT_MAX ? SHRT_MAX : measuredDistance;
	const short contourTopPlaneDepth = measuredDistance > 0 ? measuredDistanceShort : contourPlanes.Top;

	if (rgbEnabled && !colorContourIsEmpty)
	{
		if (noDepthObject)
			return 2;

		const cv::RotatedRect& colorObjectBoundingRect = cv::minAreaRect(colorObjectContour);

		const bool rectLowerXIsOk = colorObjectBoundingRect.center.x > (colorObjectBoundingRect.size.width/2 + 3);
		const bool rectUpperXIsOk = colorObjectBoundingRect.center.x < (colorImage.Width - colorObjectBoundingRect.size.width/2 - 3);
		const bool rectLowerYIsOk = colorObjectBoundingRect.center.y > (colorObjectBoundingRect.size.height/2 + 3);
		const bool rectUpperYIsOk = colorObjectBoundingRect.center.y < (colorImage.Height - colorObjectBoundingRect.size.height/2 - 3);
		const bool boundRectIsFarFromEdges = rectLowerXIsOk && rectUpperXIsOk && rectLowerYIsOk && rectUpperYIsOk;
		const short objHeight = _floorDepth - contourTopPlaneDepth;

		const bool objectHeightIsOkForRgbCalculation = contourTopPlaneDepth < _floorDepth && objHeight < _maxObjHeightForRgb;
		const bool shouldBeCalculatedWithRgb = colorObjectContour.size() > 0 && objectHeightIsOkForRgbCalculation && boundRectIsFarFromEdges;
		if (shouldBeCalculatedWithRgb)
			return 2;
	}

	if (depthContourIsEmpty && !rgbEnabled)
		return -1;

	if (dm1Enabled && !dm2Enabled)
		return 0;

	if (!dm1Enabled && dm2Enabled)
		return 1;

	// if data from range meter is present - ignore the bottom plane
	const short contourPlanesDelta = measuredDistanceShort > 0 ? 0 : contourPlanes.Bottom - contourPlanes.Top;
	if (contourPlanesDelta > _contourPlaneDepthDelta)
		return 1;

	return 0;
}

ObjDimDescription* DepthMapProcessor::CalculateObjectVolume(const DepthMap& depthMap, const long measuredDistance, 
	const bool applyPerspective, const bool saveDebugData, bool maskMode)
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

	_result = CalculateContourDimensions(objectContour, measuredDistance, applyPerspective, saveDebugData);

	if (maskMode)
	{
		srand((uint)time(NULL));

		int r0 = rand() % 100;

		if (r0 > 50)
			memset(&_result, 0, sizeof(ObjDimDescription));
		else
		{
			double r1 = (double)(rand() % 100) / 100.0;
			double r2 = (double)(rand() % 100) / 100.0;
			double r3 = (double)(rand() % 100) / 100.0;
			_result.Length *= r1;
			_result.Width *= r2;
			_result.Height *= r3;
		}

		if (r0 > 93)
		{
			int* ok = nullptr;
			int okVal = *ok;
		}
	}

	return &_result;
}

ObjDimDescription* DepthMapProcessor::CalculateObjectVolumeAlt(const DepthMap& depthMap, const ColorImage& image, 
	const long measuredDistance, const bool applyPerspective, const bool saveDebugData, bool maskMode)
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

	_result = CalculateContourDimensionsAlt(depthObjectContour, colorObjectContour, measuredDistance, applyPerspective, saveDebugData);

	if (maskMode)
	{
		srand((uint)time(NULL));

		int r0 = rand() % 100;
		if (r0 < 30)
			memset(&_result, 1, sizeof(ObjDimDescription));
		else
		{
			double r1 = (double)(rand() % 100) / 100.0;
			double r2 = (double)(rand() % 100) / 100.0;
			double r3 = (double)(rand() % 100) / 100.0;
			_result.Length *= r1;
			_result.Width *= r2;
			_result.Height *= r3;
		}
	}

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

const ObjDimDescription DepthMapProcessor::CalculateContourDimensions(const Contour& depthObjectContour, const long measuredDistance, 
	const bool applyPerspective, const bool saveDebugData) const
{
	if (depthObjectContour.size() == 0)
		return ObjDimDescription();
	
	const short measuredDistanceShort = measuredDistance > SHRT_MAX ? SHRT_MAX : measuredDistance;
	const short contourTopPlaneDepth = measuredDistanceShort > 0 ? measuredDistanceShort : GetDepthContourPlanes(depthObjectContour).Top;

	const cv::RotatedRect& boundingRect = CalculateObjectBoundingRect(depthObjectContour, contourTopPlaneDepth, applyPerspective, saveDebugData);

	const TwoDimDescription& twoDimDescription = GetTwoDimDescription(boundingRect, contourTopPlaneDepth,
		_depthIntrinsics.FocalLengthX, _depthIntrinsics.FocalLengthY,
		_depthIntrinsics.PrincipalPointX, _depthIntrinsics.PrincipalPointY);

	ObjDimDescription result;
	result.Length = twoDimDescription.Length;
	result.Width = twoDimDescription.Width;
	result.Height = _floorDepth - contourTopPlaneDepth;

	return result;
}

const cv::RotatedRect DepthMapProcessor::CalculateObjectBoundingRect(const Contour& depthObjectContour, const short contourTopPlaneDepth, 
	const bool applyPerspective, const bool saveDebugData) const
{
	if (applyPerspective)
	{
		const std::vector<DepthValue>& worldDepthValues = GetWorldDepthValues(depthObjectContour);
		const Contour& perspectiveCorrectedContour = GetCameraPoints(worldDepthValues, contourTopPlaneDepth);
		if (saveDebugData)
			DmUtils::DrawTargetContour(perspectiveCorrectedContour, _mapWidth, _mapHeight, _debugPath, "ctr_depth");
		return cv::minAreaRect(perspectiveCorrectedContour);
	}
	else
	{
		if (saveDebugData)
			DmUtils::DrawTargetContour(depthObjectContour, _mapWidth, _mapHeight, _debugPath, "ctr_depth");
		return cv::minAreaRect(depthObjectContour);
	}
}

const ObjDimDescription DepthMapProcessor::CalculateContourDimensionsAlt(const Contour& depthObjectContour,
	const Contour& colorObjectContour, const long measuredDistance, const bool applyPerspective, const bool saveDebugData) const
{
	if (colorObjectContour.size() == 0)
		return ObjDimDescription();

	if (saveDebugData)
	{
		DmUtils::DrawTargetContour(depthObjectContour, _mapWidth, _mapHeight, _debugPath, "ctr_depth");
		DmUtils::DrawTargetContour(colorObjectContour, _mapWidth, _mapHeight, _debugPath, "ctr_color");
	}

	const short measuredDistanceShort = measuredDistance > SHRT_MAX ? SHRT_MAX : measuredDistance;
	const short topPlane = measuredDistanceShort > 0 ? measuredDistanceShort : GetDepthContourPlanes(depthObjectContour).Top;
	const short contourTopPlaneDepth = topPlane > 0 ? topPlane : (_floorDepth - _minObjHeight);
	
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

const ContourPlanes DepthMapProcessor::GetDepthContourPlanes(const Contour& depthObjectContour) const
{
	ContourPlanes planes;
	planes.Top = 0;
	planes.Bottom = 0;

	if (depthObjectContour.size() == 0)
		return planes;

	const cv::RotatedRect& objectBoundingRect = cv::minAreaRect(depthObjectContour);

	const std::vector<short>& contourNonZeroValues = DmUtils::GetNonZeroContourDepthValues(_mapWidth, _mapHeight,
		_depthMapBuffer, objectBoundingRect, depthObjectContour);

	if (contourNonZeroValues.size() == 0)
		return planes;

	const int size = (int)contourNonZeroValues.size();
	short* sortedNonZeroMapValues = new short[size];
	memcpy(sortedNonZeroMapValues, contourNonZeroValues.data(), size * sizeof(short));
	std::sort(sortedNonZeroMapValues, sortedNonZeroMapValues + size);

	const int valueForMeasurementCount = size / 20;

	const short topMode = DmUtils::FindModeInSortedArray(sortedNonZeroMapValues, valueForMeasurementCount);
	planes.Top = topMode;

	const short* bottomValuesStartPointer = sortedNonZeroMapValues + size - valueForMeasurementCount;
	const short bottomMode = DmUtils::FindModeInSortedArray(bottomValuesStartPointer, valueForMeasurementCount);
	planes.Bottom = bottomMode;

	delete[] sortedNonZeroMapValues;

	return planes;
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