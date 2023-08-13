#pragma once

#include <algorithm>
#include "OpenCVInclude.h"

typedef unsigned char byte;
typedef unsigned int uint;

enum class AlgorithmSelectionStatus
{
	Undefined = -5,
	NoAlgorithmsAllowed = -3,
	DataIsInvalid = -2,
	NoObjectFound = -1,
	Dm1 = 0,
	Dm2 = 1,
	Rgb = 2,
};

struct VolumeCalculationResult
{
	int LengthMm;
	int WidthMm;
	int HeightMm;
};

struct TwoDimDescription
{
	int Length;
	int Width;
};

struct RelPoint
{
	float X;
	float Y;
};

struct AbsRect
{
	int X;
	int Y;
	int Width;
	int Height;
};

struct FlPoint
{
	float X;
	float Y;
};

struct RotAbsRect
{
	FlPoint Points[4];
	int Width;
	int Height;
	float AngleDeg;
};

struct RelRect
{
	float X;
	float Y;
	float Width;
	float Height;
};

struct RotRelRect
{
	FlPoint Points[4];
	float Width;
	float Height;
	float AngleDeg;
};

struct CameraIntrinsics
{
	float FovX;
	float FovY;
	float FocalLengthX;
	float FocalLengthY;
	float PrincipalPointX;
	float PrincipalPointY;
};

struct ColorImage
{
	int Width;
	int Height;
	byte* Data;
	byte BytesPerPixel;
};

struct DepthMap
{
	int Width;
	int Height;
	short* Data;
};

struct DepthValue
{
	int XWorld;
	int YWorld;
	short Value;
};

struct AbsPoint
{
	int X;
	int Y;
};

struct MeasurementVolume
{
	std::vector<cv::Point> Points;
	short smallerDepthValue;
	short largerDepthValue;
};

struct ContourPlanes
{
	short Top;
	short Bottom;
};

struct VolumeCalculationData
{
	const DepthMap* DepthMap;
	const ColorImage* ColorImage;
	const AlgorithmSelectionStatus SelectedAlgorithm;
	const short CalculatedDistance;
};

struct NativeAlgorithmSelectionData
{
	const DepthMap* DepthMap;
	const ColorImage* ColorImage;
	const short CalculatedDistance;
	const bool Dm1Enabled;
	const bool Dm2Enabled;
	const bool RgbEnabled;
	const char DebugFileName[128];
};

struct NativeAlgorithmSelectionResult
{
	AlgorithmSelectionStatus Status;
	int RangeMeterWasUsed;
};
