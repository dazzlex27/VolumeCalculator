#include "DmUtils.h"
#include <cmath>
#include <climits>
#include <fstream>

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
			mapData[i] = 0;
	}
}

const std::vector<short> DmUtils::GetContourDepthValues(const int mapWidth, const int mapHeight, const short*const mapData, 
	const cv::RotatedRect& roi, const Contour& contour)
{
	cv::Rect boundingRect = roi.boundingRect();

	std::vector<short> result;
	result.emplace_back(boundingRect.width * boundingRect.height);

	for (int j = boundingRect.y; j < boundingRect.y + boundingRect.height; j++)
	{
		for (int i = boundingRect.x; i < boundingRect.x + boundingRect.width; i++)
		{
			if (cv::pointPolygonTest(contour, cv::Point(i, j), false) >= 0.0)
			{
				result.emplace_back(mapData[j * mapWidth + i]);
			}
		}
	}

	return result;
}

const DepthMap*const DmUtils::ReadDepthMapFromFile(const char* filepath)
{
	std::ifstream stream(filepath);
	if (!stream.good())
		return nullptr;

	int width = 0;
	stream >> width;
	int height = 0;
	stream >> height;
	DepthMap* dm = new DepthMap(width, height);

	short value;
	int index = 0;

	while (stream >> value)
	{
		dm->Data[index++] = value;
	}

	stream.close();

	return dm;
}

void DmUtils::SaveDepthMapToFile(const std::string& filename, const DepthMap& map)
{
	std::ofstream file;
	file.open(filename);

	const int frameSize = map.Width * map.Height;

	file << map.Width;
	file << map.Height;

	for (int i = 0; i < frameSize; i++)
		file << map.Data[i] << std::endl;

	file.close();
}

const RotRelRect DmUtils::RotAbsRectToRel(const int rotWidth, const int rotHeight, const cv::RotatedRect& rect)
{
	RotRelRect result;

	cv::Point2f rectPoints[4];
	rect.points(rectPoints);

	memcpy(result.Points, rectPoints, 4 * sizeof(FlPoint));
	for (int i = 0; i < 4; i++)
	{
		result.Points[i].X = (float)rectPoints[i].x / rotWidth;
		result.Points[i].Y = (float)rectPoints[i].y / rotHeight;
	}
	result.Width = rect.size.width / rotWidth;
	result.Height = rect.size.height / rotHeight;
	result.AngleDeg = rect.angle;

	return result;
}