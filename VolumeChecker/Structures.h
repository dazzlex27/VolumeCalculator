#pragma once

#include <algorithm>

typedef unsigned char byte;
typedef unsigned int uint;

struct ObjDimDescription
{
	short Width;
	short Height;
	short Depth;
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

struct ImageFrame
{
	int Width;
	int Height;
	byte* Data;
};

struct DepthFrame
{
	int Width;
	int Height;
	short* Data;
};

struct DepthMap
{
	const int Width;
	const int Height;
	const int Length;
	short*const Data;

	DepthMap(const int width, const int height)
		:Width(width),
		Height(height),
		Length(width * height),
		Data (new short[width*height])
	{
	}

	~DepthMap()
	{
		if (Data)
			delete[] Data;
	}

	inline const short GetMaxValue()
	{
		return *(std::max_element(Data, Data + Length));
	}
};