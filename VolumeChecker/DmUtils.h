#pragma once

#pragma once

#include <vector>
#include "CVInclude.h"
#include "Structures.h"

class DmUtils
{
public:
	static const RelPoint AbsoluteToRelative(const cv::Point& abs, const int width, const int height);
	static const std::vector<Contour> GetValidContours(const std::vector<Contour>& contours, const float minAreaRatio, const int imageDataLength);
	static void ConvertDepthMapDataToImage(const short*const mapData, const int mapDataLength, byte*const imageData);
	static void ConvertImageToDepthMap(const int depthMapLength, const unsigned char* imgData, short* depthMapData);
	static void ConvertDepthMapDataToBinaryMask(const int mapDataLength, const short*const mapData, byte*const maskData);
	static void FilterDepthMap(const int mapDataLength, short*const mapData, const short value);
	static const AbsRect CalculateContourBoundingBox(const Contour& contour);
	static const std::vector<short> GetContourDepthValues(const int mapWidth, const short*const mapData, const cv::Rect& roi);
};