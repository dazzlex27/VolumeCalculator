#include "DepthMapProcessorAPI.h"
#include "DepthMapProcessor.h"

DepthMapProcessor* Processor = nullptr;

DLL_EXPORT int CreateDepthMapProcessor(const float fovX, const float fovY)
{
	if (fovX <= 0 || fovY <= 0 )
		return -1;

	if (Processor != nullptr)
		DestroyDepthMapProcessor();

	Processor = new DepthMapProcessor(fovX, fovY);

	return 0;
}

DLL_EXPORT void SetCalculatorSettings(short minDepth, short floorDepth, short cutOffDepth)
{
	Processor->SetSettings(minDepth, floorDepth, cutOffDepth);
}

DLL_EXPORT ObjDimDescription* CalculateObjectVolume(int mapWidth, int mapHeight, short* mapData)
{
	if (Processor == nullptr)
		return nullptr;

	if (mapData == nullptr)
		return nullptr;

	return Processor->CalculateObjectVolume(mapWidth, mapHeight, mapData);
}

DLL_EXPORT short CalculateFloorDepth(int mapWidth, int mapHeight, short* mapData)
{
	if (Processor == nullptr)
		return -1;

	if (mapData == nullptr)
		return -1;

	return Processor->CalculateFloorDepth(mapWidth, mapHeight, mapData);
}

DLL_EXPORT int DestroyDepthMapProcessor()
{
	if (Processor == nullptr)
		return 1;

	delete Processor;
	Processor = nullptr;

	return 0;
}