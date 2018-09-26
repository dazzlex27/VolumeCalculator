#pragma once

#include "Structures.h"

#define DLL_EXPORT extern "C" _declspec(dllexport)

DLL_EXPORT int CreateFrameFeeder();

DLL_EXPORT void SubscribeToColorFrames(ColorFrameCallback progressCallback);
DLL_EXPORT void UnsubscribeFromColorFrames(ColorFrameCallback progressCallback);

DLL_EXPORT void SubscribeToDepthFrames(DepthFrameCallback progressCallback);
DLL_EXPORT void UnsubscribeFromDepthFrames(DepthFrameCallback progressCallback);

DLL_EXPORT bool IsDeviceAvailable();

DLL_EXPORT ColorFrame* GetNextRgbFrame();

DLL_EXPORT DepthFrame* GetNextDepthFrame();

DLL_EXPORT int DestroyFrameFeeder();