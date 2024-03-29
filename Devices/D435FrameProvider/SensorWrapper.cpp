#include "SensorWrapper.h"
#include <algorithm>

const short MIN_DEPTH = 300;
const short MAX_DEPTH = 10000;

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
}

void SensorWrapper::AddColorSubscriber(ColorFrameCallback callback)
{
	_colorSubscribers.emplace_back(callback);
}

void SensorWrapper::RemoveColorSubscriber(ColorFrameCallback callback)
{
	_colorSubscribers.erase(std::remove(_colorSubscribers.begin(), _colorSubscribers.end(), callback),
		_colorSubscribers.end());
}

void SensorWrapper::AddDepthSubscriber(DepthFrameCallback callback)
{
	_depthSubscribers.emplace_back(callback);
}

void SensorWrapper::RemoveDepthSubscriber(DepthFrameCallback callback)
{
	_depthSubscribers.erase(std::remove(_depthSubscribers.begin(), _depthSubscribers.end(), callback), _depthSubscribers.end());
}

DepthCameraIntrinsics SensorWrapper::GetDepthCameraIntrinsics() const
{
	const rs2_intrinsics& i = _pipe.get_active_profile().get_stream(RS2_STREAM_DEPTH).as<rs2::video_stream_profile>().get_intrinsics();
	DepthCameraIntrinsics intrinsics;
	intrinsics.FocalLengthX = i.fx;
	intrinsics.FocalLengthY = i.fy;
	intrinsics.PrincipalPointX = i.ppx;
	intrinsics.PrincipalPointY = i.ppy;

	return intrinsics;
}

ColorFrame* SensorWrapper::GetNextColorFrame(const rs2::video_frame& videoFrame)
{
	const int frameWidth = videoFrame.get_width();
	const int frameHeight = videoFrame.get_height();

	const bool frameSizeChanged = frameWidth != _colorFrame->Width || frameHeight != _colorFrame->Height;
	if (frameSizeChanged)
	{
		if (_colorFrame->Data)
			delete[] _colorFrame->Data;

		_colorFrame->Width = frameWidth;
		_colorFrame->Height = frameHeight;
		_colorFrame->Data = new byte[frameWidth * frameHeight * sizeof(byte) * 3];
	}

	ConvertFrameToColorFrame(videoFrame, _colorFrame->Data);

	return _colorFrame;
}

DepthFrame* SensorWrapper::GetNextDepthFrame(const rs2::depth_frame& depthFrame)
{
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

void SensorWrapper::Run()
{
	_pipe.start();

	while (_running)
	{
		try
		{
			_connected = false;
			auto frameset = _pipe.wait_for_frames();

			const rs2::depth_frame& depth = frameset.get_depth_frame();
			if (depth)
			{
				_connected = true;

				if (_depthSubscribers.size() == 0)
					continue;

				DepthFrame* depthFrame = GetNextDepthFrame(depth);

				for (uint i = 0; i < _depthSubscribers.size(); i++)
					_depthSubscribers[i](depthFrame);
			}

			const rs2::video_frame& color = frameset.get_color_frame();
			if (color)
			{
				_connected = true;

				if (_colorSubscribers.size() == 0)
					continue;

				ColorFrame* depthFrame = GetNextColorFrame(color);

				for (uint i = 0; i < _colorSubscribers.size(); i++)
					_colorSubscribers[i](depthFrame);
			}

		}
		catch (std::exception ex)
		{
			_connected = false;
		}
	}
}

void SensorWrapper::ConvertFrameToColorFrame(const rs2::video_frame& frame, byte*const data)
{
	const int frameSize = frame.get_width() * frame.get_height() * 3;

	memcpy(data, frame.get_data(), frameSize);
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
			const short valueToAdd = (short)(dist_to_center * 1000);
			const short value = valueToAdd > MIN_DEPTH && valueToAdd < MAX_DEPTH ? valueToAdd : 0;
			depthValues.emplace_back(value);
		}
	}

	memcpy(data, depthValues.data(), frameSize * sizeof(short));
}