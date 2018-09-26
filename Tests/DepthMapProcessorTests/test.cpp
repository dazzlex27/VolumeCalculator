#include <iostream>
#include <DepthMapProcessorAPI.h>
#include "OpenCVInclude.h"
#include "Structures.h"
#include "Utils.h"
#include <fstream>

void FindFilteringDepth(const char* path)
{
	const DepthMap*const depthMap = Utils::ReadDepthMapFromFile(path);
	const int mapWidth = depthMap->Width;
	const int mapHeight = depthMap->Height;
	const int mapLength = depthMap->Length;

	short* dataCopy = new short[mapLength];
	memcpy(dataCopy, depthMap->Data, mapLength * sizeof(short));

	byte* binaryMask = new byte[mapLength];

	for (int i = 3000; i >= 2300; i--)
	{
		Utils::FilterDepthMap(mapLength, dataCopy, i);

		memset(binaryMask, 0, mapLength);

		for (int j = 0; j < mapHeight; j++)
		{
			for (int k = 0; k < mapWidth; k++)
			{
				const int index = j * mapWidth + k;
				if (dataCopy[index] > 0)
					binaryMask[index] = 255;
			}
		}

		cv::Mat tempMat(mapHeight, mapWidth, CV_8UC1, binaryMask);
		cv::imwrite("out/depthTest/" + std::to_string(i) + ".png", tempMat);
	}

	delete depthMap;
}

void CalculateVolume(const float focalX, const float focalY, const float principalX, const float principalY, const short minDepth, const short maxDepth,
	const short floorDepth, const short cutOffDepth, const char* path)
{
	const DepthMap* depthMap = Utils::ReadDepthMapFromFile(path);

	const int mapWidth = depthMap->Width;
	const int mapHeight = depthMap->Height;
	std::cout << "map found, " << mapWidth << "x" << mapHeight << std::endl;

	auto handle = CreateDepthMapProcessor(focalX, focalY, principalX, principalY, minDepth, maxDepth);
	SetCalculatorSettings(handle, floorDepth, cutOffDepth);

	const ObjDimDescription* desc = CalculateObjectVolume(handle, mapWidth, mapHeight, depthMap->Data);
	std::cout << "l=" << desc->Length << " w=" << desc->Width << " h=" << desc->Height << std::endl;
	std::cout << "debug info saved in out/ if the directory was present" << std::endl;
	DestroyDepthMapProcessor(handle);

	delete depthMap;
}

void TestFloorDepth()
{
	const int mapWidth = 4;
	const int mapHeight = 3;
	short* mapData = new short[4 * 3];
	memset(mapData, 0, mapWidth * mapHeight * sizeof(short));

	auto handle = CreateDepthMapProcessor(1, 1, 1, 1, 1, 1);
	const short depth = CalculateFloorDepth(handle, mapWidth, mapHeight, mapData);

	DestroyDepthMapProcessor(handle);
	delete[] mapData;
}

int main(int argc, char* argv[])
{
	TestFloorDepth();
	//CalculateVolume(70.6f, 60.0f, "table1202x728.dm", 600, 2085, 1000);

	return 0;
}