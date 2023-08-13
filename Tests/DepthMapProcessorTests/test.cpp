#include <iostream>
#include "DepthMapProcessorAPI.h"
#include "OpenCVInclude.h"
#include "Structures.h"
#include "Utils.h"
#include <fstream>
#include <DepthMapProcessor.h>
#include <chrono>
#include <numeric>

DepthMapProcessor* CreateNewProcessorHandle(const short floorDepth, const short cutOffDepth)
{
	CameraIntrinsics colorCameraIntrinsics{};
	colorCameraIntrinsics.FovX = 84.1f;
	colorCameraIntrinsics.FovY = 53.8f;
	colorCameraIntrinsics.FocalLengthX = 1081.37f;
	colorCameraIntrinsics.FocalLengthY = 1081.37f;
	colorCameraIntrinsics.PrincipalPointX = 959.5f;
	colorCameraIntrinsics.PrincipalPointY = 539.5f;

	CameraIntrinsics depthCameraIntrinsics{};
	depthCameraIntrinsics.FovX = 70.6f;
	depthCameraIntrinsics.FovY = 60.0f;
	depthCameraIntrinsics.FocalLengthX = 367.7066f;
	depthCameraIntrinsics.FocalLengthY = 367.7066f;
	depthCameraIntrinsics.PrincipalPointX = 257.8094f;
	depthCameraIntrinsics.PrincipalPointY = 207.3965f;

	RelPoint* workAreaPoints = new RelPoint[4];
	workAreaPoints[0].X = 0.2; workAreaPoints[0].Y = 0.2;
	workAreaPoints[1].X = 0.2; workAreaPoints[1].Y = 0.8;
	workAreaPoints[2].X = 0.8; workAreaPoints[2].Y = 0.8;
	workAreaPoints[3].X = 0.8; workAreaPoints[3].Y = 0.2;

	DepthMapProcessor* handle = CreateDepthMapProcessor(colorCameraIntrinsics, depthCameraIntrinsics);
	SetAlgorithmSettings(handle, floorDepth, cutOffDepth, workAreaPoints, 4, RelRect{ 0,0,0,0 });

	delete[] workAreaPoints;

	return handle;
}

void FindFilteringDepth(const char* path)
{
	const DepthMap*const depthMap = Utils::ReadDepthMapFromFile(path);
	const int mapWidth = depthMap->Width;
	const int mapHeight = depthMap->Height;
	const int mapLength = mapWidth * mapHeight;

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
	}

	delete depthMap;
}

void TestFloorDepth()
{
	const int floorDepth = 764;
	const int cutOffDepth = floorDepth - 10;
	DepthMapProcessor* handle = CreateNewProcessorHandle(floorDepth, cutOffDepth);

	const DepthMap* const depthMap = Utils::ReadDepthMapFromFile("0.dm");

	const short calculatedDepth = CalculateFloorDepth(handle, *depthMap);
	std::cout << "true depth = " << floorDepth << ", calculated depth = " << calculatedDepth << std::endl;

	DestroyDepthMapProcessor(handle);
	delete[] depthMap;
}

void TestVolumeCalculation()
{
	const DepthMap* const depthMap = Utils::ReadDepthMapFromFile("0.dm");

	cv::Mat image = cv::imread("0.png", cv::IMREAD_COLOR);
	ColorImage colorImage{};
	colorImage.Width = image.cols;
	colorImage.Height = image.rows;
	colorImage.BytesPerPixel = 3;
	colorImage.Data = image.data;

	const int floorDepth = 764;
	const int cutOffDepth = floorDepth - 10;
	DepthMapProcessor* handle = CreateNewProcessorHandle(floorDepth, cutOffDepth);

	VolumeCalculationData data{ depthMap, &colorImage, AlgorithmSelectionStatus::Dm1, -1 };

	const int numTests = 10000;
	std::vector<int> resultsStubStorage;
	resultsStubStorage.reserve(numTests);

	std::vector<int> runTimes;
	runTimes.reserve(numTests);

	int testsRun = 0;

	std::cout << "starting performance test..." << std::endl;

	// warmup
	const VolumeCalculationResult* const result = CalculateObjectVolume(handle, data);

	while (testsRun < numTests)
	{
		const auto begin = std::chrono::steady_clock::now();
		const VolumeCalculationResult* const result = CalculateObjectVolume(handle, data);
		const auto end = std::chrono::steady_clock::now();

		//std::cout << result->LengthMm << " " << result->WidthMm << " " << result->HeightMm << std::endl;
		resultsStubStorage.emplace_back(result->LengthMm); // to avoid optimization

		const auto msElapsed = std::chrono::duration_cast<std::chrono::milliseconds>(end - begin).count();
		runTimes.emplace_back((int)msElapsed);

		testsRun++;
	}

	const long minTime = *std::min_element(runTimes.begin(), runTimes.end());
	const long maxTime = *std::max_element(runTimes.begin(), runTimes.end());
	const float avgTime = std::accumulate(runTimes.begin(), runTimes.end(), 0) / (float)numTests;
	const float potentialFps = 1000.0f / avgTime;

	std::cout << "finished performance test:" << std::endl;
	std::cout << "image size: " << image.cols << "x" << image.rows << std::endl;
	std::cout << "min processing time: " << minTime << " ms" << std::endl;
	std::cout << "max processing time: " << maxTime << " ms" << std::endl;
	std::cout << "avg processing time: " << avgTime << " ms" << " (fps=" << potentialFps << ")" << std::endl;
	std::cout << std::endl;

	DestroyDepthMapProcessor(handle);
	delete depthMap;
}

int main(int argc, char* argv[])
{
	TestFloorDepth();
	TestVolumeCalculation();

	std::cout << std::endl << "press any button to exit" << std::endl;
	std::cin.get();

	return 0;
}
