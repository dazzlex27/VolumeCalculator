#pragma once

typedef unsigned char byte;

struct ColorFrame
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