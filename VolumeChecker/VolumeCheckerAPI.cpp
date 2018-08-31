#include "VolumeCheckerAPI.h"
#include "VolumeChecker.h"
#include <stdexcept>

VolumeChecker* Checker;

DLL_EXPORT int CreateVolumeChecker(const float fovX, const float fovY)
{
	if (fovX <= 0 || fovY <= 0 )
		return -1;

	if (Checker != nullptr)
		DestroyVolumeChecker();

	Checker = new VolumeChecker(fovX, fovY);

	return 0;
}

DLL_EXPORT void SetCheckerSettings(short floorDepth, short cutOffDepth)
{
	Checker->SetSettings(floorDepth, cutOffDepth);
}

DLL_EXPORT ObjDimDescription* CheckVolume(int mapWidth, int mapHeight, short* mapData)
{
	ObjDimDescription* test = new ObjDimDescription();
	test->Width = 3;
	test->Height = 4;
	test->Depth = 8;

	return test;

	if (Checker == nullptr)
		throw std::logic_error("The checker was not initialized");

	if (mapData == nullptr)
		throw std::invalid_argument("mapData was null");

	return Checker->GetVolume(mapWidth, mapHeight, mapData);
}

DLL_EXPORT int DestroyVolumeChecker()
{
	if (Checker == nullptr)
		return 1;

	delete Checker;
	Checker = nullptr;

	return 0;
}