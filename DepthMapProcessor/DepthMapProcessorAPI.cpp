#include "DepthMapProcessorAPI.h"
#include "DepthMapProcessor.h"
#include <fstream>

DepthMapProcessor* Processor;

DLL_EXPORT void CreateDepthMapProcessor(CameraIntrinsics colorIntrinsics, CameraIntrinsics depthIntrinsics)
{
	std::ofstream myFile;
	myFile.open("debug.txt");

	myFile << std::endl;

	myFile.close();

	Processor = new DepthMapProcessor(colorIntrinsics, depthIntrinsics);
}

DLL_EXPORT void SetAlgorithmSettings(short floorDepth, short cutOffDepth, RelPoint* polygonPoints, int polygonPointCount, 
	RelRect colorRoiRect)
{
	Processor->SetAlgorithmSettings(floorDepth, cutOffDepth, polygonPoints, polygonPointCount, colorRoiRect);
}

DLL_EXPORT void SetDebugPath(const char* path)
{
	Processor->SetDebugPath(path);
}

DLL_EXPORT VolumeCalculationResult* CalculateObjectVolume(VolumeCalculationData calculationData)
{
	VolumeCalculationResult* volume = Processor->CalculateObjectVolume(calculationData);
	   
	std::ofstream myFile;
	myFile.open("debug.txt", std::ios::app);

	myFile << volume->LengthMm << " " << volume->WidthMm << " " << volume->HeightMm << std::endl;

	myFile.close();

	return volume;
}

void DisposeCalculationResult(VolumeCalculationResult* result)
{
	if (result)
	{
		delete result;
		result = 0;
	}
}

DLL_EXPORT short CalculateFloorDepth(DepthMap depthMap)
{
	if (depthMap.Data == nullptr)
		return -1;

	return Processor->CalculateFloorDepth(depthMap);
}

DLL_EXPORT int SelectAlgorithm(DepthMap depthMap, ColorImage colorImage, const long measuredDistance,
	bool dm1Enabled, bool dm2Enabled, bool rgbEnabled)
{
	if (depthMap.Data == nullptr)
		return -1;

	return Processor->SelectAlgorithm(depthMap, colorImage, measuredDistance, dm1Enabled, dm2Enabled, rgbEnabled);
}

DLL_EXPORT void DestroyDepthMapProcessor()
{
	delete Processor;
	Processor = nullptr;
}