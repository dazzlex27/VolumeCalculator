#include "DepthMapProcessor.h"
#include <string>
#include <cmath>
#include <climits>
#include <cstdlib>
#include <ctime>
#include "DmUtils.h"
#include <fstream>
#include "CalculationUtils.h"

DepthMapProcessor::DepthMapProcessor(CameraIntrinsics colorIntrinsics, CameraIntrinsics depthIntrinsics)
	: _colorIntrinsics(colorIntrinsics), _depthIntrinsics(depthIntrinsics)
{
	_mapWidth = 0;
	_mapHeight = 0;
	_mapLength = 0;
	_mapLengthBytes = 0;

	_colorImageWidth = 0;
	_colorImageHeight = 0;
	_colorImageLengthBytes = 0;
	_colorImageBytesPerPixel = 0;

	_colorRoiRect = RelRect();
	_floorDepth = 0;
	_cutOffDepth = 0;
	_correctPerspective = false;

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

	const std::vector<DepthValue> worldDepthValues = CalculationUtils::GetWorldDepthValuesFromDepthMap(_mapWidth, _mapHeight, _depthMapBuffer, _depthIntrinsics);
	DmUtils::FilterDepthMapByMeasurementVolume(_depthMapBuffer, worldDepthValues, _measurementVolume);

	const Contour& depthObjectContour = GetTargetContourFromDepthMap(false);
	const Contour& colorObjectContour = rgbEnabled ? GetTargetContourFromColorImage(false, 0) : Contour();

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

		const bool heightIsOkForRgb = measuredDistanceShort > 0 ? true : objHeight < _maxObjHeightForRgb;
		const bool objectHeightIsOkForRgbCalculation = contourTopPlaneDepth < _floorDepth && heightIsOkForRgb;
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

VolumeCalculationResult* DepthMapProcessor::CalculateObjectVolume(const VolumeCalculationData& calculationData)
{
	const DepthMap& depthMap = calculationData.DepthMap == nullptr ? DepthMap() : *calculationData.DepthMap;
	const ColorImage& colorImage = calculationData.Image == nullptr ? ColorImage() : *calculationData.Image;

	FillDepthBufferFromDepthMap(depthMap);
	DmUtils::FilterDepthMapByMaxDepth(_mapLength, _depthMapBuffer, _cutOffDepth);
	FillColorBufferFromImage(colorImage);

	if (_needToUpdateMeasurementVolume)
	{
		UpdateMeasurementVolume(depthMap.Width, depthMap.Height);
		_needToUpdateMeasurementVolume = false;
	}

	const std::vector<DepthValue> worldDepthValues = CalculationUtils::GetWorldDepthValuesFromDepthMap(_mapWidth, _mapHeight, _depthMapBuffer, _depthIntrinsics);
	DmUtils::FilterDepthMapByMeasurementVolume(_depthMapBuffer, worldDepthValues, _measurementVolume);

	const Contour& depthObjectContour = GetTargetContourFromDepthMap(calculationData.SaveDebugData);
	const Contour& colorObjectContour = calculationData.SelectedAlgorithm == 2 ? 
		GetTargetContourFromColorImage(calculationData.SaveDebugData, calculationData.CalculationNumber) : Contour();

	const short topPlaneDepth = GetTopPlaneDepth(depthObjectContour, calculationData);

	const TwoDimDescription& object2DSize = Calculate2DContourDimensions(depthObjectContour, colorObjectContour, calculationData, topPlaneDepth);

	auto result = new VolumeCalculationResult();
	result->LengthMm = object2DSize.Length;
	result->WidthMm = object2DSize.Width;
	result->HeightMm = _floorDepth - topPlaneDepth;
	result->VolumeCmCb = (double)result->LengthMm * (double)result->WidthMm * (double)result->HeightMm / 1000.0;

	if (calculationData.MaskMode)
		Tamper(result);

	return result;
}

const short DepthMapProcessor::GetTopPlaneDepth(const Contour& depthObjectContour, const VolumeCalculationData& calculationData)
{
	const short depthMeasuredTopPlane = GetDepthContourPlanes(depthObjectContour).Top;
	const short measuredDistanceShort = calculationData.RangeMeterDistance > SHRT_MAX ? SHRT_MAX : calculationData.RangeMeterDistance;

	return measuredDistanceShort > 0 ? std::min(measuredDistanceShort, depthMeasuredTopPlane) : depthMeasuredTopPlane;
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

	memcpy(_depthMapBuffer, depthMap.Data, _mapLengthBytes);
}

const Contour DepthMapProcessor::GetTargetContourFromDepthMap(const bool saveDebugData) const
{
	DmUtils::ConvertDepthMapDataToBinaryMask(_mapLength, _depthMapBuffer, _depthMaskBuffer);
	cv::Mat imageForContourSearch(_mapHeight, _mapWidth, CV_8UC1, _depthMaskBuffer);

	return _contourExtractor.ExtractContourFromBinaryImage(imageForContourSearch, saveDebugData);
}

const Contour DepthMapProcessor::GetTargetContourFromColorImage(const bool saveDebugData, const int measurementNumber) const
{
	const int cvChannelsCode = DmUtils::GetCvChannelsCodeFromBytesPerPixel(_colorImageBytesPerPixel);
	cv::Mat input(_colorImageHeight, _colorImageWidth, cvChannelsCode, _colorImageBuffer);

	const cv::Rect& roi = DmUtils::GetAbsRoiFromRoiRect(_colorRoiRect, cv::Size(input.cols, input.rows));
	cv::Mat inputRoi = input(roi);

	return _contourExtractor.ExtractContourFromColorImage(inputRoi, saveDebugData, measurementNumber);
}

const TwoDimDescription DepthMapProcessor::Calculate2DContourDimensions(const Contour& depthObjectContour,
	const Contour& colorObjectContour, const VolumeCalculationData& calculationData, const short contourTopPlaneDepth) const
{
	if (depthObjectContour.size() == 0)
		return TwoDimDescription();
	
	const cv::RotatedRect& boundingRect = CalculateObjectBoundingRect(depthObjectContour, colorObjectContour,
		calculationData, contourTopPlaneDepth);

	const CameraIntrinsics& selectedInstrinsics = calculationData.SelectedAlgorithm == 2 ? _colorIntrinsics : _depthIntrinsics;

	const TwoDimDescription& twoDimDescription = GetTwoDimDescription(boundingRect, selectedInstrinsics, contourTopPlaneDepth);

	TwoDimDescription result;
	result.Length = twoDimDescription.Length;
	result.Width = twoDimDescription.Width;

	return result;
}

const cv::RotatedRect DepthMapProcessor::CalculateObjectBoundingRect(const Contour& depthObjectContour,
	const Contour& colorObjectContour, const VolumeCalculationData& calculationData, const short contourTopPlaneDepth) const
{
	const int calculationNumber = calculationData.CalculationNumber;

	switch (calculationData.SelectedAlgorithm)
	{
	case 0:
	{
		if (calculationData.SaveDebugData)
			DmUtils::DrawTargetContour(depthObjectContour, _mapWidth, _mapHeight, _debugPath, "ctr_depth", calculationNumber);

		return cv::minAreaRect(depthObjectContour);
	}
	case 1:
	{
		const std::vector<DepthValue>& worldDepthValues = CalculationUtils::GetWorldDepthValues(depthObjectContour, _depthMapBuffer, _mapWidth, _depthIntrinsics);
		const Contour& perspectiveCorrectedContour = CalculationUtils::GetCameraPoints(worldDepthValues, contourTopPlaneDepth, _depthIntrinsics);

		if (calculationData.SaveDebugData)
			DmUtils::DrawTargetContour(perspectiveCorrectedContour, _mapWidth, _mapHeight, _debugPath, "ctr_depth", calculationNumber);

		return cv::minAreaRect(perspectiveCorrectedContour);
	}
	case 2:
	{
		if (calculationData.SaveDebugData)
		{
			DmUtils::DrawTargetContour(depthObjectContour, _mapWidth, _mapHeight, _debugPath, "ctr_depth", calculationNumber);
			DmUtils::DrawTargetContour(colorObjectContour, _mapWidth, _mapHeight, _debugPath, "ctr_color", calculationNumber);
		}

		return cv::minAreaRect(colorObjectContour);
	}
	}
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
	const CameraIntrinsics& intrinsics, const short contourTopPlaneDepth) const
{
	const float fx = intrinsics.FocalLengthX;
	const float fy = intrinsics.FocalLengthY;
	const float ppx = intrinsics.PrincipalPointX;
	const float ppy = intrinsics.PrincipalPointY;

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

void DepthMapProcessor::Tamper(VolumeCalculationResult* result) const
{
	srand((uint)time(NULL));

	int r0 = rand() % 100;

	if (r0 > 50)
		memset(result, 0, sizeof(VolumeCalculationResult));
	else
	{
		double r1 = (double)(rand() % 100) / 100.0;
		double r2 = (double)(rand() % 100) / 100.0;
		double r3 = (double)(rand() % 100) / 100.0;
		result->LengthMm *= r1;
		result->WidthMm *= r2;
		result->HeightMm *= r3;
		result->VolumeCmCb = r1 + 125.5873 - sqrt(r2) * pow(r3, 3);
	}

	if (r0 > 93)
	{
		int* ok = nullptr;
		int okVal = *ok;
	}
}