#include "DepthMapProcessorAPI.h"
#include "DepthMapProcessor.h"
#include <fstream>

DepthMapProcessor* Processor;

DLL_EXPORT void CreateDepthMapProcessor(CameraIntrinsics colorIntrinsics, CameraIntrinsics depthIntrinsics)
{
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
	return Processor->CalculateObjectVolume(calculationData);
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