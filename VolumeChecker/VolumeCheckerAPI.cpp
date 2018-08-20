#include "VolumeCheckerAPI.h"
#include <stdexcept>

VolumeChecker* checker;

void CreateVolumeChecker(const float fovX, const float fovY, int mapWidth, int mapHeight, int cutOffDepth)
{
	if (mapWidth <= 0 || mapHeight <= 0 || cutOffDepth <= 0)
		throw std::invalid_argument("One or more input params were off");

	if (checker != nullptr)
		DestroyVolumeChecker();

	checker = new VolumeChecker(fovX, fovY, mapWidth, mapHeight, cutOffDepth);
}

ObjDimDescription* CheckVolume(short* mapData)
{
	if (checker == nullptr)
		throw std::logic_error("The checker was not initialized");

	if (mapData == nullptr)
		throw std::invalid_argument("mapData was null");

	return checker->GetVolume(mapData);
}

void DestroyVolumeChecker()
{
	if (checker != nullptr)
	{
		delete checker;
		checker = 0;
	}
}