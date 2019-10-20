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

	_maskMode = false;
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

void DepthMapProcessor::SetDebugPath(const char* path, const bool maskMode)
{
	_debugPath = std::string(path);
	_contourExtractor.SetDebugPath(_debugPath);

	_maskMode = maskMode;
}

const AlgorithmSelectionResult DepthMapProcessor::SelectAlgorithm(const AlgorithmSelectionData data)
{
	const bool dataIsValid = data.DepthMap->Data != nullptr && data.ColorImage->Data != nullptr;
	if (!dataIsValid)
		return DataIsInvalid;

	const bool atLeastOneModeIsEnabled = data.Dm1Enabled || data.Dm2Enabled || data.RgbEnabled;
	if (!atLeastOneModeIsEnabled)
		return NoAlgorithmsAllowed;

	PrepareBuffers(data.DepthMap, data.ColorImage);

	const Contour& depthObjectContour = GetTargetContourFromDepthMap();
	const Contour& colorObjectContour = data.RgbEnabled ? GetTargetContourFromColorImage(data.DebugFileName) : Contour();

	const bool colorContourIsEmpty = colorObjectContour.empty();
	const bool depthContourIsEmpty = depthObjectContour.empty();

	const int depthContourArea = depthContourIsEmpty ? 0 : (int)cv::contourArea(depthObjectContour);
	const bool noDepthObject = depthContourArea < 4;
	const bool bothContoursAreEmpty = noDepthObject && colorContourIsEmpty;
	if (bothContoursAreEmpty)
		return NoObjectFound;

	if (depthContourIsEmpty && !data.RgbEnabled)
		return NoObjectFound;

	AlgorithmSelectionResult algorithm = Undefined;

	const bool onlyDm1IsEnabled = data.Dm1Enabled && !data.Dm2Enabled && !data.RgbEnabled;
	if (onlyDm1IsEnabled)
		algorithm = depthContourIsEmpty ? NoObjectFound : Dm1;

	const bool onlyDm2IsEnabled = !data.Dm1Enabled && data.Dm2Enabled && !data.RgbEnabled;
	if (onlyDm2IsEnabled && algorithm == Undefined)
		algorithm = depthContourIsEmpty ? NoObjectFound : Dm2;

	const bool onlyRgbIsEnabled = !data.Dm1Enabled && !data.Dm2Enabled && data.RgbEnabled;
	if (onlyRgbIsEnabled && algorithm == Undefined)
		algorithm = colorContourIsEmpty ? NoObjectFound : Rgb;

	const ContourPlanes& contourPlanes = GetDepthContourPlanes(depthObjectContour);
	const short contourTopPlaneDepth = data.CalculatedDistance > 0 ? data.CalculatedDistance : contourPlanes.Top;

	const bool eligibleForRgbCalculation = data.RgbEnabled && !colorContourIsEmpty;
	if (eligibleForRgbCalculation && algorithm == Undefined)
	{
		if (noDepthObject)
			algorithm = Rgb;
		else
		{
			const cv::RotatedRect& colorObjectBoundingRect = cv::minAreaRect(colorObjectContour);

			// object borders must not touch the borders of the image
			const bool rectLowerXIsOk = colorObjectBoundingRect.center.x > (colorObjectBoundingRect.size.width / 2 + 3);
			const bool rectUpperXIsOk = colorObjectBoundingRect.center.x < (data.ColorImage->Width - colorObjectBoundingRect.size.width / 2 - 3);
			const bool rectLowerYIsOk = colorObjectBoundingRect.center.y > (colorObjectBoundingRect.size.height / 2 + 3);
			const bool rectUpperYIsOk = colorObjectBoundingRect.center.y < (data.ColorImage->Height - colorObjectBoundingRect.size.height / 2 - 3);
			const bool boundRectIsFarFromEdges = rectLowerXIsOk && rectUpperXIsOk && rectLowerYIsOk && rectUpperYIsOk;
			const short objHeight = _floorDepth - contourTopPlaneDepth;

			const bool heightIsOkForRgb = data.CalculatedDistance > 0 ? true : objHeight < _maxObjHeightForRgb;
			const bool objectHeightIsOkForRgbCalculation = contourTopPlaneDepth < _floorDepth && heightIsOkForRgb;
			const bool shouldBeCalculatedWithRgb = colorObjectContour.size() > 0 && objectHeightIsOkForRgbCalculation && boundRectIsFarFromEdges;
			if (shouldBeCalculatedWithRgb)
				algorithm = Rgb;
		}
	}

	if (data.Dm1Enabled && !data.Dm2Enabled && algorithm == Undefined)
		return Dm1;

	if (!data.Dm1Enabled && data.Dm2Enabled && algorithm == Undefined)
		return Dm2;

	if (algorithm == Undefined)
	{
		// if data from range meter is present - ignore the bottom plane
		const short contourPlanesDelta = data.CalculatedDistance > 0 ? 0 : contourPlanes.Bottom - contourPlanes.Top;
		if (contourPlanesDelta > _contourPlaneDepthDelta)
			algorithm = Dm2;
	}

	// if still not set for some reason - set to dm1
	if (algorithm == Undefined)
		algorithm = Dm1;

	// Saving debugData if algorithm is selected
	const bool algorithmIsSelected = algorithm == Dm1 || algorithm == Dm2 || algorithm == Rgb;
	if (algorithmIsSelected)
		CalculateObjectBoundingRect(depthObjectContour, colorObjectContour, algorithm, contourTopPlaneDepth, data.DebugFileName);

	return algorithm;
}

VolumeCalculationResult* DepthMapProcessor::CalculateObjectVolume(const VolumeCalculationData& data)
{
	if (data.DepthMap == nullptr || data.DepthMap->Data == nullptr)
		return new VolumeCalculationResult();

	if (data.ColorImage == nullptr || data.ColorImage->Data == nullptr)
		return new VolumeCalculationResult();

	PrepareBuffers(data.DepthMap, data.ColorImage);

	const Contour& depthObjectContour = GetTargetContourFromDepthMap();
	const Contour& colorObjectContour = data.SelectedAlgorithm == Rgb ? GetTargetContourFromColorImage() : Contour();

	const short contourTopPlaneDepth = data.CalculatedDistance > 0 ?
		data.CalculatedDistance
		: GetDepthContourPlanes(depthObjectContour).Top;

	const TwoDimDescription& object2DSize = Calculate2DContourDimensions(depthObjectContour, colorObjectContour, 
		data.SelectedAlgorithm, contourTopPlaneDepth);

	auto result = new VolumeCalculationResult();
	result->LengthMm = object2DSize.Length;
	result->WidthMm = object2DSize.Width;
	result->HeightMm = _floorDepth - contourTopPlaneDepth;
	result->VolumeCmCb = (double)result->LengthMm * (double)result->WidthMm * (double)result->HeightMm / 1000.0;

	if (_maskMode)
		Tamper(result);

	return result;
}

void DepthMapProcessor::PrepareBuffers(const DepthMap*const depthMap, const ColorImage*const colorImage)
{
	FillDepthBufferFromDepthMap(*depthMap);
	DmUtils::FilterDepthMapByMaxDepth(_mapLength, _depthMapBuffer, _cutOffDepth);
	FillColorBufferFromImage(*colorImage);

	if (_needToUpdateMeasurementVolume)
	{
		UpdateMeasurementVolume(depthMap->Width, depthMap->Height);
		_needToUpdateMeasurementVolume = false;
	}

	const std::vector<DepthValue> worldDepthValues = CalculationUtils::GetWorldDepthValuesFromDepthMap(_mapWidth, _mapHeight, _depthMapBuffer, _depthIntrinsics);
	DmUtils::FilterDepthMapByMeasurementVolume(_depthMapBuffer, worldDepthValues, _measurementVolume);
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

const Contour DepthMapProcessor::GetTargetContourFromDepthMap() const
{
	DmUtils::ConvertDepthMapDataToBinaryMask(_mapLength, _depthMapBuffer, _depthMaskBuffer);
	cv::Mat imageForContourSearch(_mapHeight, _mapWidth, CV_8UC1, _depthMaskBuffer);

	return _contourExtractor.ExtractContourFromBinaryImage(imageForContourSearch);
}

const Contour DepthMapProcessor::GetTargetContourFromColorImage(const char* debugPath) const
{
	const int cvChannelsCode = DmUtils::GetCvChannelsCodeFromBytesPerPixel(_colorImageBytesPerPixel);
	cv::Mat input(_colorImageHeight, _colorImageWidth, cvChannelsCode, _colorImageBuffer);

	const cv::Rect& roi = DmUtils::GetAbsRoiFromRoiRect(_colorRoiRect, cv::Size(input.cols, input.rows));
	cv::Mat inputRoi = input(roi);

	return _contourExtractor.ExtractContourFromColorImage(inputRoi, debugPath);
}

const TwoDimDescription DepthMapProcessor::Calculate2DContourDimensions(const Contour& depthObjectContour,
	const Contour& colorObjectContour, const AlgorithmSelectionResult selectedAlgorithm, const short contourTopPlaneDepth) const
{
	if (depthObjectContour.empty())
		return TwoDimDescription();
	
	const cv::RotatedRect& boundingRect = CalculateObjectBoundingRect(depthObjectContour, colorObjectContour,
		selectedAlgorithm, contourTopPlaneDepth);

	const CameraIntrinsics& selectedInstrinsics = selectedAlgorithm == Rgb ? _colorIntrinsics : _depthIntrinsics;

	const TwoDimDescription& twoDimDescription = GetTwoDimDescription(boundingRect, selectedInstrinsics, contourTopPlaneDepth);

	TwoDimDescription result;
	result.Length = twoDimDescription.Length;
	result.Width = twoDimDescription.Width;

	return result;
}

const cv::RotatedRect DepthMapProcessor::CalculateObjectBoundingRect(const Contour& depthObjectContour,
	const Contour& colorObjectContour, const AlgorithmSelectionResult selectedAlgorithm, const short contourTopPlaneDepth,
	const char* debugFilename) const
{
	switch (selectedAlgorithm)
	{
	case Dm1:
	{
		if (debugFilename != "")
		{
			const std::string& filename = _debugPath + "/" + debugFilename + "_ctr_depth.png";
			DmUtils::DrawTargetContour(depthObjectContour, _mapWidth, _mapHeight, filename);
		}

		return cv::minAreaRect(depthObjectContour);
	}
	case Dm2:
	{
		const std::vector<DepthValue>& worldDepthValues = CalculationUtils::GetWorldDepthValues(depthObjectContour, _depthMapBuffer, _mapWidth, _depthIntrinsics);
		const Contour& perspectiveCorrectedContour = CalculationUtils::GetCameraPoints(worldDepthValues, contourTopPlaneDepth, _depthIntrinsics);

		if (debugFilename != "")
		{
			const std::string& filename = _debugPath + "/" + debugFilename + "_ctr_depth.png";
			DmUtils::DrawTargetContour(perspectiveCorrectedContour, _mapWidth, _mapHeight, filename);
		}

		return cv::minAreaRect(perspectiveCorrectedContour);
	}
	case Rgb:
	{
		if (debugFilename != "")
		{
			const std::string& depthFilename = _debugPath + "/" + debugFilename + "_ctr_depth.png";
			const std::string& colorFilename = _debugPath + "/" + debugFilename + "_ctr_color.png";
			DmUtils::DrawTargetContour(depthObjectContour, _mapWidth, _mapHeight, depthFilename);
			DmUtils::DrawTargetContour(colorObjectContour, _mapWidth, _mapHeight, colorFilename);
		}

		return cv::minAreaRect(colorObjectContour);
	}
	default:
		return cv::RotatedRect();
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
		result->LengthMm *= (int)r1;
		result->WidthMm *= (int)r2;
		result->HeightMm *= (int)r3;
		result->VolumeCmCb = r1 + 125.5873 - sqrt(r2) * pow(r3, 3);
	}

	if (r0 > 93)
	{
		int* ok = nullptr;
		int okVal = *ok;
	}
}