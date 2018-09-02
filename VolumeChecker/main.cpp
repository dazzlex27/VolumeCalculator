#include <iostream>
#include "VolumeCheckerAPI.h"
#include "CVInclude.h"
#include "DmUtils.h"
#include "Structures.h"
#include "DmUtils.h"
#include <fstream>

void FindFilteringDepth()
{
	const DepthMap*const depthMap = DmUtils::ReadDepthMapFromFile("in/box1.txt");
	const int mapWidth = depthMap->Width;
	const int mapHeight = depthMap->Height;
	const int mapLength = depthMap->Length;

	short* dataCopy = new short[mapLength];
	memcpy(dataCopy, depthMap->Data, mapLength * sizeof(short));

	byte* binaryMask = new byte[mapLength];

	for (int i = 1300; i >= 900; i--)
	{
		DmUtils::FilterDepthMap(mapLength, dataCopy, i);

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

void CalculateVolume(int argc, char* argv[])
{
	//if (argc < 4)
	//{
	//	std::cout << "Usage: " << argv[0] << " inputfile.txt floorDepth cutOffDepth" << std::endl;
	//	return;
	//}

	//const char* filepath = argv[1];
	//const DepthMap* depthMap = DmUtils::ReadDepthMapFromFile(filepath);
	//if (depthMap == nullptr)
	//{
	//	std::cout << "Error! Cannot open " << filepath << std::endl;
	//	return;
	//}

	const DepthMap* depthMap = DmUtils::ReadDepthMapFromFile("in/close_box0.txt");
	
	const int mapWidth = depthMap->Width;
	const int mapHeight = depthMap->Height;
	std::cout << "map found, " << mapWidth << "x" << mapHeight << std::endl;

	//const int floorDepth = atoi(argv[2]);
	//const int cutOffDepth = atoi(argv[3]);

	const int floorDepth = 467;
	const int cutOffDepth = 450;
	CreateVolumeChecker(86, 57);

	const ObjDimDescription* desc = CalculateVolume(mapWidth, mapHeight, depthMap->Data);
	std::cout << "w=" << desc->Width << " h=" << desc->Height << " d=" << desc->Depth << std::endl;
	std::cout << "debug info saved in out/ if the directory was present" << std::endl;

	delete depthMap;
}

void main(int argc, char* argv[])
{
}