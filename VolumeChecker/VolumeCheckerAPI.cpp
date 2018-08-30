#include "VolumeCheckerAPI.h"
#include "VolumeChecker.h"
#include <stdexcept>

VolumeChecker* checker;

DLL_EXPORT int TestExport()
{
	return -1337;
}

DLL_EXPORT int CreateVolumeChecker(const float fovX, const float fovY, int mapWidth, int mapHeight, int floorDepth, int cutOffDepth)
{
	if (mapWidth <= 0 || mapHeight <= 0 || cutOffDepth <= 0)
		return -1;

	if (checker != nullptr)
		DestroyVolumeChecker();

	checker = new VolumeChecker(fovX, fovY, mapWidth, mapHeight, floorDepth, cutOffDepth);

	return 0;
}

DLL_EXPORT ImageFrame* GetNextRgbFrame()
{
	return nullptr;
}

DLL_EXPORT DepthFrame* GetNextDepthFrame()
{
	return nullptr;
}

DLL_EXPORT ObjDimDescription* CheckVolume(short* mapData)
{
	ObjDimDescription* test = new ObjDimDescription();
	test->Width = 3;
	test->Height = 4;
	test->Depth = 8;

	return test;

	if (checker == nullptr)
		throw std::logic_error("The checker was not initialized");

	if (mapData == nullptr)
		throw std::invalid_argument("mapData was null");

	return checker->GetVolume(mapData);
}

DLL_EXPORT int DestroyVolumeChecker()
{
	if (checker != nullptr)
	{
		delete checker;
		checker = 0;

		return 1;
	}

	return 0;
}