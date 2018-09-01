#include "SensorWrapper.h"
#include <iostream>

int GLOBAL_COUNT = 0;

void ReceiveDepthFrame(DepthFrame* frame)
{
	std::cout << "frame " << GLOBAL_COUNT++ << " received (" << frame->Width << " " << frame->Height << std::endl;
}

int main()
{
	DepthFrameCallback callback = ReceiveDepthFrame;

	SensorWrapper s;
	s.AddDepthSubscriber(callback);

	while (true) {}

	return 0;
}

