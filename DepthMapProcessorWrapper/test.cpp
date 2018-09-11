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

void CalculateVolume(const float fovX, const float fovY, const char* path, const short minDepth, 
	const short floorDepth, const short cutOffDepth)
{
	const DepthMap* depthMap = Utils::ReadDepthMapFromFile(path);

	const int mapWidth = depthMap->Width;
	const int mapHeight = depthMap->Height;
	std::cout << "map found, " << mapWidth << "x" << mapHeight << std::endl;

	CreateDepthMapProcessor(fovX, fovY);
	SetCalculatorSettings(minDepth, floorDepth, cutOffDepth);

	const ObjDimDescription* desc = CalculateObjectVolume(mapWidth, mapHeight, depthMap->Data);
	std::cout << "w=" << desc->Width << " h=" << desc->Height << " d=" << desc->Depth << std::endl;
	std::cout << "debug info saved in out/ if the directory was present" << std::endl;

	delete depthMap;
}

int main(int argc, char* argv[])
{
	CalculateVolume(70.6f, 60.0f, "0.dm", 600, 686, 670);

	return 0;
}