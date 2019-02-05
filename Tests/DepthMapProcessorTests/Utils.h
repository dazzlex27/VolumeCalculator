#pragma once

#include <cstring>
#include <string>
#include "Structures.h"

class Utils
{
public:
	static const DepthMap*const ReadDepthMapFromFile(const char* filename);
	static void SaveDepthMapToFile(const std::string& filename, const DepthMap& map);
	static void FilterDepthMap(const int mapDataLength, short*const mapData, const short value);
};