#pragma once

typedef unsigned char byte;
typedef unsigned int uint;

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

typedef void(__stdcall * ColorFrameCallback)(ColorFrame*);

typedef void(__stdcall * DepthFrameCallback)(DepthFrame*);