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

DLL_EXPORT void SetCheckerSettings(short minDepth, short floorDepth, short cutOffDepth)
{
	Checker->SetSettings(minDepth, floorDepth, cutOffDepth);
}

DLL_EXPORT ObjDimDescription* CalculateVolume(int mapWidth, int mapHeight, short* mapData)
{
	if (Checker == nullptr)
		throw std::logic_error("The checker was not initialized");

	if (mapData == nullptr)
		throw std::invalid_argument("mapData was null");

	return Checker->CalculateVolume(mapWidth, mapHeight, mapData);
}

DLL_EXPORT short CalculateFloorDepth(int mapWidth, int mapHeight, short* mapData)
{
	if (Checker == nullptr)
		throw std::logic_error("The checker was not initialized");

	if (mapData == nullptr)
		throw std::invalid_argument("mapData was null");

	return Checker->CalculateFloorDepth(mapWidth, mapHeight, mapData);
}

DLL_EXPORT ObjDimDescription* CheckVolumeFromStereo(int mapWidth, int mapHeight, short * mapData1, short * mapData2, int offsetXmm, int offsetYmm)
{
	if (Checker == nullptr)
		throw std::logic_error("The checker was not initialized");

	if (mapData1 == nullptr || mapData2 == nullptr)
		throw std::invalid_argument("mapData was null");

	return Checker->GetVolumeFromStereo(mapWidth, mapHeight, mapData1, mapData2, offsetXmm, offsetYmm);
}

DLL_EXPORT int DestroyVolumeChecker()
{
	if (Checker == nullptr)
		return 1;

	delete Checker;
	Checker = nullptr;

	return 0;
}