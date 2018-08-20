#pragma once

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

struct RelRect
{
	float X;
	float Y;
	float Width;
	float Height;
};