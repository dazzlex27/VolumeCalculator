#include "DepthMapProcessorAPI.h"
#include "DepthMapProcessor.h"

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

DLL_EXPORT void SetDebugPath(const char* path, bool maskMode)
{
	Processor->SetDebugPath(path, maskMode);
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

DLL_EXPORT NativeAlgorithmSelectionResult* SelectAlgorithm(NativeAlgorithmSelectionData data)
{
	return Processor->SelectAlgorithm(data);
}

void DisposeAlgorithmSelectionResult(VolumeCalculationResult* result)
{
	if (result)
	{
		delete result;
		result = 0;
	}
}

DLL_EXPORT void DestroyDepthMapProcessor()
{
	delete Processor;
	Processor = nullptr;
}