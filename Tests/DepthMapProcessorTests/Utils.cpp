#include "Utils.h"
#include <fstream>

const DepthMap*const Utils::ReadDepthMapFromFile(const char* filepath)
{
	std::ifstream stream(filepath);
	if (!stream.good())
		return nullptr;

	auto dm = new DepthMap();
	stream >> dm->Width;
	stream >> dm->Height;
	dm->Data = new short[dm->Width * dm->Height];

	short value;
	int index = 0;

	while (stream >> value)
		dm->Data[index++] = value;

	stream.close();

	return dm;
}

void Utils::SaveDepthMapToFile(const std::string& filename, const DepthMap& map)
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

void Utils::FilterDepthMap(const int mapDataLength, short*const mapData, const short value)
{
	for (int i = 0; i < mapDataLength; i++)
	{
		if (mapData[i] > value)
			mapData[i] = 0;
	}
}
