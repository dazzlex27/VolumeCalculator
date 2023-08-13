#include "DepthMapProcessorAPI.h"
#include "DepthMapProcessor.h"

DLL_EXPORT DepthMapProcessor* CreateDepthMapProcessor(CameraIntrinsics colorIntrinsics, CameraIntrinsics depthIntrinsics)
{
	return new DepthMapProcessor(colorIntrinsics, depthIntrinsics);
}

DLL_EXPORT void SetAlgorithmSettings(DepthMapProcessor* processor, short floorDepth, short cutOffDepth, RelPoint* polygonPoints, int polygonPointCount,
	RelRect colorRoiRect)
{
	processor->SetAlgorithmSettings(floorDepth, cutOffDepth, polygonPoints, polygonPointCount, colorRoiRect);
}

DLL_EXPORT void SetDebugDirectory(DepthMapProcessor* processor, const char* path)
{
	processor->SetDebugDirectory(path);
}

DLL_EXPORT VolumeCalculationResult* CalculateObjectVolume(DepthMapProcessor* processor, VolumeCalculationData calculationData)
{
	return processor->CalculateObjectVolume(calculationData);
}

void DisposeCalculationResult(VolumeCalculationResult* result)
{
	if (result)
	{
		delete result;
		result = 0;
	}
}

DLL_EXPORT short CalculateFloorDepth(DepthMapProcessor* processor, DepthMap depthMap)
{
	if (depthMap.Data == nullptr)
		return -1;

	return processor->CalculateFloorDepth(depthMap);
}

DLL_EXPORT NativeAlgorithmSelectionResult* SelectAlgorithm(DepthMapProcessor* processor, NativeAlgorithmSelectionData data)
{
	return processor->SelectAlgorithm(data);
}

void DisposeAlgorithmSelectionResult(VolumeCalculationResult* result)
{
	if (result)
	{
		delete result;
		result = 0;
	}
}

DLL_EXPORT void DestroyDepthMapProcessor(DepthMapProcessor* processor)
{
	delete processor;
	processor = nullptr;
}
