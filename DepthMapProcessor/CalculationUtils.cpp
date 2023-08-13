#include "CalculationUtils.h"

const std::vector<DepthValue> CalculationUtils::GetWorldDepthValues(const Contour& objectContour, const short*const depthMapBuffer,
	const int mapWidth, const CameraIntrinsics& intrinsics)
{
	const double PI = 3.14159265359;

	std::vector<DepthValue> depthValues;
	depthValues.reserve(objectContour.size());

	const cv::Moments& m = cv::moments(objectContour);
	const int cx = (int)(m.m10 / m.m00);
	const int cy = (int)(m.m01 / m.m00);

	for (int i = 0; i < objectContour.size(); i++)
	{
		const int contourPointX = objectContour[i].x;
		const int contourPointY = objectContour[i].y;

		const int deltaX = contourPointX - cx;
		const int deltaY = contourPointY - cy;

		const double pointAngle = atan2(-deltaY, deltaX) * 180 / PI;

		int offsetX = 0;
		int offsetY = 0;

		if (pointAngle >= -20 && pointAngle < 20)
		{
			offsetX = -1;
			offsetY = 0;
		}
		else if (pointAngle >= 20 && pointAngle < 70)
		{
			offsetX = -1;
			offsetY = -1;
		}
		else if (pointAngle >= 70 && pointAngle < 110)
		{
			offsetX = 0;
			offsetY = 1;
		}
		else if (pointAngle >= 110 && pointAngle <= 160)
		{
			offsetX = 1;
			offsetY = 1;
		}
		else if (pointAngle < -20 && pointAngle >= -70)
		{
			offsetX = -1;
			offsetY = 0;
		}
		else if (pointAngle < -70 && pointAngle >= 110)
		{
			offsetX = 1;
			offsetY = -1;
		}
		else if (pointAngle < 110 && pointAngle >= -160)
		{
			offsetX = 0;
			offsetY = -1;
		}
		else // [160,180] || [-160, -180]
		{
			offsetX = 1;
			offsetY = 0;
		}

		const int depthBufferIndex = contourPointY * mapWidth + contourPointX;
		const short pointDepth = depthMapBuffer[depthBufferIndex];

		std::vector<short> borderDepthValues;
		int tempX = contourPointX;
		int tempY = contourPointY;

		for (int j = 0; j < 5; j++)
		{
			tempX += offsetX;
			tempY += offsetY;

			const int offsetIndex = tempY * mapWidth + tempX;
			const short offsetDepthValue = depthMapBuffer[offsetIndex];
			if (offsetDepthValue < pointDepth)
				borderDepthValues.emplace_back(offsetDepthValue);
		}

		int elementSum = 0;
		const uint borderValuesCount = (const uint)borderDepthValues.size();
		for (uint j = 0; j < borderValuesCount; j++)
			elementSum += borderDepthValues[j];
		elementSum = borderValuesCount > 0 ? elementSum / borderValuesCount : 0;

		const short contourModeValue = elementSum > 0 ? elementSum : pointDepth;

		const int xWorld = (int)((contourPointX + 1 - intrinsics.PrincipalPointX) * contourModeValue / intrinsics.FocalLengthX);
		const int yWorld = (int)(-(contourPointY + 1 - intrinsics.PrincipalPointY) * contourModeValue / intrinsics.FocalLengthY);

		DepthValue depthValue;
		depthValue.XWorld = xWorld;
		depthValue.YWorld = yWorld;
		depthValue.Value = contourModeValue;

		depthValues.emplace_back(depthValue);
	}

	return depthValues;
}

const std::vector<cv::Point> CalculationUtils::GetCameraPoints(const std::vector<DepthValue>& depthValues, const short targetDepth,
	const CameraIntrinsics& intrinsics)
{
	std::vector<cv::Point> cameraPoints;
	cameraPoints.reserve(depthValues.size());

	for (int i = 0; i < depthValues.size(); i++)
	{
		const int xWorld = depthValues[i].XWorld;
		const int yWorld = depthValues[i].YWorld;

		const int contourPointX = (int)(xWorld * intrinsics.FocalLengthX / targetDepth + intrinsics.PrincipalPointX - 1);
		const int contourPointY = (int)(-(yWorld * intrinsics.FocalLengthY / targetDepth) + intrinsics.PrincipalPointY - 1);

		cv::Point cameraPoint;
		cameraPoint.x = contourPointX;
		cameraPoint.y = contourPointY;
		cameraPoints.emplace_back(cameraPoint);
	}

	return cameraPoints;
}

void CalculationUtils::GetWorldDepthValuesFromDepthMap(const int mapWidth, const int mapHeight, 
	const short*const depthMapBuffer, const CameraIntrinsics& intrinsics, DepthValue*const worldDepthValues)
{
	int throughIndex = 0;

	for (int j = 0; j < mapHeight; j++)
	{
		for (int i = 0; i < mapWidth; i++)
		{
			const short depth = depthMapBuffer[j * mapWidth + i];
			const int xWorld = (int)((i + 1 - intrinsics.PrincipalPointX) * depth / intrinsics.FocalLengthX);
			const int yWorld = (int)(-(j + 1 - intrinsics.PrincipalPointY) * depth / intrinsics.FocalLengthY);

			DepthValue& depthValue = worldDepthValues[throughIndex++];
			depthValue.Value = depth;
			depthValue.XWorld = xWorld;
			depthValue.YWorld = yWorld;
		}
	}
}
