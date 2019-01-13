#include "DepthMapProcessorAPI.h"
#include "DepthMapProcessor.h"

DLL_EXPORT void* CreateDepthMapProcessor(ColorCameraIntristics colorIntrinsics, DepthCameraIntristics depthIntrinsics)
{
	return new DepthMapProcessor(colorIntrinsics, depthIntrinsics);
}

DLL_EXPORT void SetAlgorithmSettings(void* handle, short floorDepth, short cutOffDepth, RelPoint* polygonPoints, int polygonPointCount, 
	RelRect colorRoiRect)
{
	auto processor = (DepthMapProcessor*)handle;
	if (processor == nullptr)
		return;

	processor->SetAlgorithmSettings(floorDepth, cutOffDepth, polygonPoints, polygonPointCount, colorRoiRect);
}

DLL_EXPORT void SetDebugPath(void* handle, const char* path)
{
	auto processor = (DepthMapProcessor*)handle;
	if (processor == nullptr)
		return;

	processor->SetDebugPath(path);
}

DLL_EXPORT ObjDimDescription* CalculateObjectVolume(void* handle, DepthMap depthMap, bool saveDebugData)
{
	auto processor = (DepthMapProcessor*)handle;
	if (processor == nullptr)
		return nullptr;

	if (depthMap.Data == nullptr)
		return nullptr;

	return processor->CalculateObjectVolume(depthMap, saveDebugData);
}

DLL_EXPORT ObjDimDescription* CalculateObjectVolumeAlt(void* handle, DepthMap depthMap, ColorImage image, bool saveDebugData)
{
	auto processor = (DepthMapProcessor*)handle;
	if (processor == nullptr)
		return nullptr;

	if (image.Data == nullptr)
		return nullptr;

	if (depthMap.Data == nullptr)
		return nullptr;

	return processor->CalculateObjectVolumeAlt(depthMap, image, saveDebugData);
}

DLL_EXPORT short CalculateFloorDepth(void* handle, DepthMap depthMap)
{
	auto processor = (DepthMapProcessor*)handle;
	if (processor == nullptr)
		return -1;

	if (depthMap.Data == nullptr)
		return -1;

	return processor->CalculateFloorDepth(depthMap);
}

DLL_EXPORT void DestroyDepthMapProcessor(void* handle)
{
	auto processor = (DepthMapProcessor*)handle;
	if (processor == nullptr)
		return;

	delete processor;
	processor = nullptr;
}