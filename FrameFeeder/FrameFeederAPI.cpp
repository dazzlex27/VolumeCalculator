#include "FrameFeederAPI.h"

#include <librealsense2/rs.hpp>
#include "SensorWrapper.h"

SensorWrapper* Wrapper;

DLL_EXPORT int CreateFrameFeeder()
{
	if (Wrapper != nullptr)
		DestroyFrameFeeder();

	Wrapper = new SensorWrapper();

	return 0;
}

DLL_EXPORT bool IsDeviceAvailable()
{
	return Wrapper->IsSensorAvailable();
}

DLL_EXPORT ColorFrame* GetNextRgbFrame()
{
	return Wrapper->GetNextRgbFrame();
}

DLL_EXPORT DepthFrame* GetNextDepthFrame()
{
	return Wrapper->GetNextDepthFrame();
}

DLL_EXPORT int DestroyFrameFeeder()
{
	if (Wrapper == nullptr)
		return 1;

	delete Wrapper;
	Wrapper = nullptr;
}