#include "DepthMapProcessorAPI.h"
#include "DepthMapProcessor.h"

DLL_EXPORT void* CreateDepthMapProcessor(ColorCameraIntristics colorIntrinsics, DepthCameraIntristics depthIntrinsics)
{
	//if (focalLengthX <= 0 || focalLengthY <= 0 || principalX <= 0 || principalY <= 0 || minDepth <= 0 || maxDepth <= 0)
	//	return nullptr;

	return new DepthMapProcessor(colorIntrinsics, depthIntrinsics);
}

DLL_EXPORT void SetCalculatorSettings(void* handle, short floorDepth, short cutOffDepth)
{
	auto processor = (DepthMapProcessor*)handle;
	processor->SetSettings(floorDepth, cutOffDepth);
}

DLL_EXPORT ObjDimDescription* CalculateObjectVolume(void* handle, int mapWidth, int mapHeight, short* mapData)
{
	auto processor = (DepthMapProcessor*)handle;

	if (processor == nullptr)
		return nullptr;

	if (mapData == nullptr)
		return nullptr;

	return processor->CalculateObjectVolume(mapWidth, mapHeight, mapData);
}

DLL_EXPORT ObjDimDescription* CalculateObjectVolumeAlt(void* handle, int imageWidth, int imageHeight, byte* imageData, 
	int bytesPerPixel, RelRect roiRect, int mapWidth, int mapHeight, short* mapData)
{
	auto processor = (DepthMapProcessor*)handle;

	if (processor == nullptr)
		return nullptr;

	if (imageData == nullptr)
		return nullptr;

	if (mapData == nullptr)
		return nullptr;

	return processor->CalculateObjectVolumeAlt(imageWidth, imageHeight, imageData, bytesPerPixel, roiRect, mapWidth, mapHeight, mapData);
}

DLL_EXPORT short CalculateFloorDepth(void* handle, int mapWidth, int mapHeight, short* mapData)
{
	auto processor = (DepthMapProcessor*)handle;

	if (processor == nullptr)
		return -1;

	if (mapData == nullptr)
		return -1;

	return processor->CalculateFloorDepth(mapWidth, mapHeight, mapData);
}

DLL_EXPORT void DestroyDepthMapProcessor(void* handle)
{
	auto processor = (DepthMapProcessor*)handle;

	if (processor == nullptr)
		return;

	delete processor;
	processor = nullptr;
}