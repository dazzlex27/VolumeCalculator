#pragma once

#include <librealsense2/rs.hpp>
#include <thread>
#include "Structures.h"

class SensorWrapper
{
private:
	rs2::context _context;
	rs2::pipeline _pipe;
	rs2::frame_queue _frameQueue;
	std::thread _queueThread;

	bool _running;
	bool _connected;

	ColorFrame* _colorFrame;
	DepthFrame* _depthFrame;

public:
	SensorWrapper();
	~SensorWrapper();

	bool IsSensorAvailable() const { return _connected; }

	ColorFrame* GetNextRgbFrame();
	DepthFrame* GetNextDepthFrame();

private:
	void Run();
	void ConvertFrameToDepthFrame(const rs2::depth_frame& frame, short*const data);
};