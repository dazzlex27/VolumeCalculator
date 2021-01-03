#pragma once

#include "Structures.h"
#include "OpenCVInclude.h"

class DmUtils
{
public:
	static const RelPoint AbsoluteToRelative(const cv::Point& abs, const int width, const int height);
	static const std::vector<Contour> GetValidContours(const std::vector<Contour>& contours, const float minAreaRatio, const int imageDataLength);
	static void ConvertDepthMapDataToBinaryMask(const int mapDataLength, const short*const mapData, byte*const maskData);
	static void FilterDepthMapByMaxDepth(const int mapDataLength, short*const mapData, const short value);
	static void FilterDepthMapByMeasurementVolume(short*const mapData, const std::vector<DepthValue>& worldDepthValues, 
		const MeasurementVolume& volume);
	static const std::vector<short> GetNonZeroContourDepthValues(const DepthMap& depthMap);
	static const std::vector<short> GetNonZeroContourDepthValues(const int mapWidth, const int mapHeight, const short*const mapData,
		const cv::RotatedRect& roi, const Contour& contour);
	static const RotRelRect RotAbsRectToRel(const int rotWidth, const int rotHeight, const cv::RotatedRect& rect);
	static const float GetDistanceBetweenPoints(const int x1, const int y1, const int x2, const int y2);
	static const cv::Rect GetAbsRoiFromRoiRect(const RelRect& roiRect, const cv::Size& frameSize);
	static const int GetCvChannelsCodeFromBytesPerPixel(const int bytesPerPixel);
	static const short FindModeInSortedArray(const short*const array, const int count);
	static void DrawTargetContour(const Contour& contour, const int width, const int height, const std::string& filename);
	static bool IsPointInZone(const DepthValue& worldPoint, const MeasurementVolume& volume);
	static bool IsPointInsidePolygon(const std::vector<cv::Point>& polygon, int x, int y);
	static bool IsObjectInBounds(const Contour& objectContour, const int width, const int height);
};