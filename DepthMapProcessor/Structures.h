#pragma once

#include <algorithm>
#include "OpenCVInclude.h"

typedef unsigned char byte;
typedef unsigned int uint;

struct VolumeCalculationResult
{
	int LengthMm;
	int WidthMm;
	int HeightMm;
	double VolumeCmCb;
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
	DepthMap* DepthMap;
	ColorImage* Image;
	int SelectedAlgorithm;
	long long RangeMeterDistance;
	bool SaveDebugData;
	int CalculationNumber;
	bool MaskMode;
};