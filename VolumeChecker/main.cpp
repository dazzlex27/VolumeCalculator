#include <iostream>
#include "VolumeCheckerAPI.h"
#include "CVInclude.h"
#include "DmUtils.h"
#include <librealsense2/rs.hpp>
#include "Structures.h"
#include "DmUtils.h"
#include <fstream>

int GLOBAL_INDEX = 0;

void SaveFrameToDisk(const rs2::depth_frame& frame, const std::string& filepath)
{
	const int width = frame.get_width();
	const int height = frame.get_height();
	int frameSize = width * height;
	std::vector<short> depthValues;
	depthValues.reserve(frameSize);

	for (int j = 0; j < height; j++)
	{
		for (int i = 0; i < width; i++)
		{
			float dist_to_center = frame.get_distance(i, j);
			const short value = (short)(dist_to_center * 1000);
			depthValues.emplace_back(value);
		}
	}

	DepthMap map(width, height);
	memcpy(map.Data, depthValues.data(), frameSize * sizeof(short));

	DmUtils::SaveDepthMapToFile(filepath, map);
}

void SaveInputFrames(int argc, char* argv[])
{
	if (argc < 2)
	{
		std::cout << "Usage: " << argv[0] << " outputFolder" << std::endl;
		return;
	}

	// Create a Pipeline, which serves as a top-level API for streaming and processing frames
	rs2::pipeline p;

	// Configure and start the pipeline
	rs2::pipeline_profile selection = p.start();

	float minValue = FLT_MAX_EXP;
	float maxValue = -1.0f;

	bool gotFrames = false;

	while (true)
	{
		// Block program until frames arrive
		rs2::frameset frames = p.wait_for_frames();

		// Try to get a frame of a depth image
		rs2::depth_frame depth = frames.get_depth_frame();
		// The frameset might not contain a depth frame, if so continue until it does
		if (!depth)
			continue;

		if (!gotFrames)
		{
			std::cout << "started receiving frames..." << std::endl;
			gotFrames = true;
		}

		// Get the depth frame's dimensions
		float width = depth.get_width();
		float height = depth.get_height();
		std::string filepath(std::string(argv[1]) + "/" + std::to_string(GLOBAL_INDEX) + ".txt");

		std::cout << "file " << filepath <<  " saved" << std::endl;
		SaveFrameToDisk(depth, filepath);
		GLOBAL_INDEX++;
		
		// Query the distance from the camera to the object in the center of the image
		float dist_to_center = depth.get_distance(width / 2, height / 2);

		if (dist_to_center > maxValue)
			maxValue = dist_to_center;

		if (dist_to_center < minValue)
			minValue = dist_to_center;

		// Print the distance 
		std::cout << "current = " << dist_to_center << " (min = " << minValue << " max = " << maxValue << ")\r";
	}
}

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
	const int mapLength = depthMap->Length;
	std::cout << "map found, " << mapWidth << "x" << mapHeight << std::endl;

	//const int floorDepth = atoi(argv[2]);
	//const int cutOffDepth = atoi(argv[3]);

	const int floorDepth = 467;
	const int cutOffDepth = 450;
	CreateVolumeChecker(86, 57, mapWidth, mapHeight, floorDepth, cutOffDepth);

	const ObjDimDescription* desc = CheckVolume(depthMap->Data);
	std::cout << "w=" << desc->Width << " h=" << desc->Height << " d=" << desc->Depth << std::endl;
	std::cout << "debug info saved in out/ if the directory was present" << std::endl;

	delete depthMap;
}

void main(int argc, char* argv[])
{
	FindFilteringDepth();
}