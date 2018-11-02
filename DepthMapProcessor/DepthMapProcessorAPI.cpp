#include "DepthMapProcessorAPI.h"
#include "DepthMapProcessor.h"

DLL_EXPORT void* CreateDepthMapProcessor(ColorCameraIntristics colorIntrinsics, DepthCameraIntristics depthIntrinsics)
{
	return new DepthMapProcessor(colorIntrinsics, depthIntrinsics);
}

DLL_EXPORT void SetAlgorithmSettings(void* handle, short floorDepth, short cutOffDepth, RelRect colorRoiRect)
{
	auto processor = (DepthMapProcessor*)handle;
	processor->SetAlgorithmSettings(floorDepth, cutOffDepth, colorRoiRect);
}

DLL_EXPORT ObjDimDescription* CalculateObjectVolume(void* handle, DepthMap depthMap)
{
	auto processor = (DepthMapProcessor*)handle;

	if (processor == nullptr)
		return nullptr;

	if (depthMap.Data == nullptr)
		return nullptr;

	return processor->CalculateObjectVolume(depthMap);
}

DLL_EXPORT ObjDimDescription* CalculateObjectVolumeAlt(void* handle, DepthMap depthMap, ColorImage image)
{
	auto processor = (DepthMapProcessor*)handle;

	if (processor == nullptr)
		return nullptr;

	if (image.Data == nullptr)
		return nullptr;

	if (depthMap.Data == nullptr)
		return nullptr;

	return processor->CalculateObjectVolumeAlt(depthMap, image);
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