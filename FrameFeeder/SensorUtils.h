#pragma once

#include <librealsense2/rs.hpp>

void SaveFrameToDisk(const rs2::depth_frame& frame, const std::string& filepath);

void SaveInputFrames(int argc, char* argv[]);

