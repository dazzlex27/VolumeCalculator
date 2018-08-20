#include <iostream>
#include "VolumeCheckerAPI.h"
#include "CVInclude.h"
#include "DmUtils.h"

int main()
{
	const float fovX = 58.5f;
	const float fovY = 46.6f;

	cv::Mat img = cv::imread("synth1.png");
	const int mapWidth = img.cols;
	const int mapHeight = img.rows;
	const int mapLength = mapWidth * mapHeight;
	short* mapData = new short[mapLength];
	DmUtils::ConvertImageToDepthMap(mapLength, img.data, mapData);

	const int cutOffDepth = 2000;

	CreateVolumeChecker(fovX, fovY, mapWidth, mapHeight, cutOffDepth);

	ObjDimDescription* descr = CheckVolume(mapData);

	std::cout << "w=" << descr->Width << " h=" << descr->Height << " d=" << descr->Depth << std::endl;

	DestroyVolumeChecker();

	getchar();
		
	return 0;
}