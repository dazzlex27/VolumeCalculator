#include "DmUtils.h"
#include <cmath>
#include <climits>

const std::vector<Contour> DmUtils::GetValidContours(const std::vector<Contour>& contours, const float minAreaRatio, const int imageDataLength)
{
	const int minContourAreaPixels = (const int)(imageDataLength * minAreaRatio);
	std::vector<Contour> contoursValid;
	contoursValid.reserve(contours.size());
	for (int i = 0; i < contours.size(); i++)
	{
		if (contourArea(contours[i]) >= minContourAreaPixels)
			contoursValid.emplace_back(Contour(contours[i]));
	}

	return contoursValid;
}

const RelPoint DmUtils::AbsoluteToRelative(const cv::Point& abs, const int width, const int height)
{
	RelPoint res;
	res.X = (float)(abs.x) / width;
	res.Y = (float)(abs.y) / height;

	return res;
}

void DmUtils::ConvertDepthMapDataToImage(const short*const mapData, const int mapDataLength, byte*const imageData)
{
	for (int s = 0, d = 0; s < mapDataLength; s++, d += 3)
	{
		const short v = mapData[s] / 10;
		int vi = (int)v;
		if (vi > 765 || vi < 0)
			vi = 0;
		if (vi == 0)
			vi = 765;
		vi = 765 - vi;

		//инвертируем, чтобы не было выбросов вверх при усреднении на границах между неопределенными зонами и объектами
		imageData[d] = (byte)std::min(vi, 255);
		imageData[d + 1] = (byte)std::max(std::min(vi - 255, 255), 0);
		imageData[d + 2] = (byte)std::max(std::min(vi - 510, 255), 0);
	}
}

void DmUtils::ConvertImageToDepthMap(const int depthMapLength, const unsigned char* imgData, short* depthMapData)
{
	for (int i = 0, d = 0; i < depthMapLength; i++, d += 3)
	{
		const short depthValue = imgData[d] + imgData[d + 1] + imgData[d + 2];
		depthMapData[i] = depthValue < 50 ? 0 : (765 - depthValue) * 10;
	}
}

void DmUtils::ConvertDepthMapDataToBinaryMask(const int mapDataLength, const short*const mapData,  byte*const maskData)
{
	for (int i = 0; i < mapDataLength; i++)
	{
		short mapValue = mapData[i];
		maskData[i] = mapValue > 0 ? 255 : 0;
	}
}

void DmUtils::FilterDepthMap(const int mapDataLength, short*const mapData,  const short value)
{
	for (int i = 0; i < mapDataLength; i++)
	{
		if (mapData[i] > value)
			mapData[i] = -1;
	}
}

const AbsRect DmUtils::CalculateContourBoundingBox(const Contour& contour)
{
	int left = INT32_MAX;
	int top = INT32_MAX;
	int right = -1;
	int bottom = -1;

	for (const cv::Point& point : contour)
	{
		if (point.x < left)
			left = point.x;
		if (point.x > right)
			right = point.x;
		if (point.y < top)
			top = point.y;
		if (point.y > bottom)
			bottom = point.y;
	}

	const int x = left;
	const int y = top;
	const int width = right - left;
	const int height = bottom - top;

	return AbsRect{ x, y, width, height };
}

const std::vector<short> DmUtils::GetContourDepthValues(const int mapWidth, const short*const mapData, const cv::Rect& roi)
{
	std::vector<short> result;
	result.reserve(roi.width * roi.height);

	for (int j = 0; j < roi.height; j++)
	{
		int yIndex = mapWidth * roi.y + mapWidth * j;

		for (int i = 0; i < roi.width; i++)
		{
			int xIndex = roi.x + i;

			short value = mapData[yIndex + xIndex];

			if (value > 0)
				result.emplace_back(value);
		}
	}

	return result;
}
