#include "D435FrameProviderAPI.h"

#include <librealsense2/rs.hpp>
#include "SensorWrapper.h"

SensorWrapper* Wrapper;

DLL_EXPORT int CreateFrameProvider()
{
	if (Wrapper != nullptr)
		DestroyFrameProvider();

	Wrapper = new SensorWrapper();

	return 0;
}

DLL_EXPORT void SubscribeToColorFrames(ColorFrameCallback callback)
{
	Wrapper->AddColorSubscriber(callback);
}

DLL_EXPORT void UnsubscribeFromColorFrames(ColorFrameCallback callback)
{
	Wrapper->RemoveColorSubscriber(callback);
}

DLL_EXPORT void SubscribeToDepthFrames(DepthFrameCallback callback)
{
	Wrapper->AddDepthSubscriber(callback);
}

DLL_EXPORT void UnsubscribeFromDepthFrames(DepthFrameCallback callback)
{
	Wrapper->RemoveDepthSubscriber(callback);
}

DLL_EXPORT bool IsDeviceAvailable()
{
	return Wrapper->IsSensorAvailable();
}

DLL_EXPORT int DestroyFrameProvider()
{
	if (Wrapper == nullptr)
		return 1;

	delete Wrapper;
	Wrapper = nullptr;

	return 0;
}