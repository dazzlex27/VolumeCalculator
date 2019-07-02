using Primitives;
using System;

namespace FrameProviders
{
	public interface IFrameProvider : IDisposable
	{
		event Action<ImageData> ColorFrameReady;
		event Action<DepthMap> DepthFrameReady;

		event Action<ImageData> UnrestrictedColorFrameReady;
		event Action<DepthMap> UnrestrictedDepthFrameReady;

		double ColorCameraFps { get; set; }

		double DepthCameraFps { get; set; }

		ColorCameraParams GetColorCameraParams();

		DepthCameraParams GetDepthCameraParams();

		void Start();

		void SuspendColorStream();

		void ResumeColorStream();

		void SuspendDepthStream();

		void ResumeDepthStream();
	}
}