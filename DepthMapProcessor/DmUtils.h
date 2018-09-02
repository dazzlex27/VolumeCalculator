#pragma once

#include "Structures.h"
#include "OpenCVInclude.h"

class DmUtils
{
public:
	static const RelPoint AbsoluteToRelative(const cv::Point& abs, const int width, const int height);
	static const std::vector<Contour> GetValidContours(const std::vector<Contour>& contours, const float minAreaRatio, const int imageDataLength);
	static void ConvertDepthMapDataToImage(const short*const mapData, const int mapDataLength, byte*const imageData);
	static void ConvertImageToDepthMap(const int depthMapLength, const unsigned char* imgData, short* depthMapData);
	static void ConvertDepthMapDataToBinaryMask(const int mapDataLength, const short*const mapData, byte*const maskData);
	static void FilterDepthMap(const int mapDataLength, short*const mapData, const short value);
	static const std::vector<short> GetContourDepthValues(const int mapWidth, const int mapHeight, const short*const mapData, 
		const cv::RotatedRect& roi, const Contour& contour);
	static const DepthMap*const ReadDepthMapFromFile(const char* filename);
	static void SaveDepthMapToFile(const std::string& filename, const DepthMap& map);
	static const RotRelRect RotAbsRectToRel(const int rotWidth, const int rotHeight, const cv::RotatedRect& rect);
};