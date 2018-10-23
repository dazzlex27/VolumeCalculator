#pragma once

#include <librealsense2/rs.hpp>
#include <vector>
#include <thread>
#include "Structures.h"

class SensorWrapper
{
private:
	rs2::context _context;
	rs2::pipeline _pipe;
	std::thread _queueThread;
	std::vector<ColorFrameCallback> _colorSubscribers;
	std::vector<DepthFrameCallback> _depthSubscribers;

	bool _running;
	bool _connected;

	ColorFrame* _colorFrame;
	DepthFrame* _depthFrame;

public:
	SensorWrapper();
	~SensorWrapper();

	bool IsSensorAvailable() const { return _connected; }

	void AddColorSubscriber(ColorFrameCallback callback);
	void RemoveColorSubscriber(ColorFrameCallback callback);

	void AddDepthSubscriber(DepthFrameCallback callback);
	void RemoveDepthSubscriber(DepthFrameCallback callback);

	DepthCameraIntrinsics GetDepthCameraIntrinsics() const;

	ColorFrame* GetNextColorFrame(const rs2::video_frame& videoFrame);
	DepthFrame* GetNextDepthFrame(const rs2::depth_frame& depthFrame);

private:
	void Run();
	void ConvertFrameToDepthFrame(const rs2::depth_frame& frame, short*const data);
	void ConvertFrameToColorFrame(const rs2::video_frame& frame, byte*const data);
};