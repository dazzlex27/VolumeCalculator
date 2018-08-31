#include "SensorWrapper.h"

SensorWrapper::SensorWrapper()
{
	_colorFrame = new ColorFrame();
	memset(_colorFrame, 0, sizeof(ColorFrame));
	_depthFrame = new DepthFrame();
	memset(_colorFrame, 0, sizeof(DepthFrame));

	_running = true;
	_connected = false;
	_queueThread = std::thread(&SensorWrapper::Run, this);
	_queueThread.detach();
}

SensorWrapper::~SensorWrapper()
{
	_running = false;
	_queueThread.join();
}

ColorFrame* SensorWrapper::GetNextRgbFrame()
{
	if (!_connected)
		return nullptr;

	rs2::frameset frameset;
	if (_frameQueue.poll_for_frame(&frameset))
	{
		const rs2::depth_frame& colorFrame = frameset.get_color_frame();

		_colorFrame->Width = colorFrame.get_width();
		_colorFrame->Height = colorFrame.get_height();
		//_colorFrame->Data = ;
		auto data = colorFrame.get_data();

		return _colorFrame;
	}

	return nullptr;
}

DepthFrame* SensorWrapper::GetNextDepthFrame()
{
	if (!_connected)
		return nullptr;

	rs2::frameset frameset;
	if (_frameQueue.poll_for_frame(&frameset))
	{
		const rs2::depth_frame& depthFrame = frameset.get_depth_frame();

		const int frameWidth = depthFrame.get_width();
		const int frameHeight = depthFrame.get_height();

		const bool frameSizeChanged = frameWidth != _depthFrame->Width || frameHeight != _depthFrame->Height;
		if (frameSizeChanged)
		{
			if (_depthFrame->Data)
				delete[] _depthFrame->Data;

			_depthFrame->Width = frameWidth;
			_depthFrame->Height = frameHeight;
			_depthFrame->Data = new short[frameWidth * frameHeight * sizeof(short)];
		}

		ConvertFrameToDepthFrame(depthFrame, _depthFrame->Data);
		
		return _depthFrame;
	}

	return nullptr;
}

#include <iostream>

void SensorWrapper::Run()
{
	_pipe.start();
	_frameQueue = rs2::frame_queue(2);

	while (_running)
	{
		try
		{
			auto frameset = _pipe.wait_for_frames();
			rs2::depth_frame depth = frameset.get_depth_frame();
			// The frameset might not contain a depth frame, if so continue until it does
			if (!depth)
			{
				_connected = false;
				continue;
			}

			rs2::depth_frame colorFrame = frameset.get_depth_frame();
			auto d1 = colorFrame.get_data();
			float* data = (float*)colorFrame.get_data();
			for (int i = 0; i < colorFrame.get_width() * colorFrame.get_height(); i++)
			{
				std::cout << data[i];
				getchar();
			}


			_frameQueue.enqueue(frameset);
			_connected = true;
		}
		catch (std::exception ex)
		{
			_connected = false;
		}
	}
}

void SensorWrapper::ConvertFrameToDepthFrame(const rs2::depth_frame& frame, short*const data)
{
	const int width = frame.get_width();
	const int height = frame.get_height();
	const int frameSize = width * height;
	std::vector<short> depthValues;
	depthValues.reserve(frameSize);

	for (int j = 0; j < height; j++)
	{
		for (int i = 0; i < width; i++)
		{
			float dist_to_center = frame.get_distance(i, j);
			const short value = (short)(dist_to_center * 1000);
			depthValues.emplace_back(value);
		}
	}

	memcpy(data, depthValues.data(), frameSize * sizeof(short));
}