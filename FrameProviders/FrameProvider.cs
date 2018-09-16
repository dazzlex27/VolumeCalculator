using System;
using Common;

namespace FrameProviders
{
	public abstract class FrameProvider : IDisposable
	{
		public event Action<ImageData> ColorFrameReady;
		public event Action<DepthMap> DepthFrameReady;

		public abstract DeviceParams GetDeviceParams();

		public abstract void Start();
		public abstract void Dispose();

		public abstract void SuspendColorStream();
		public abstract void ResumeColorStream();

		public abstract void SuspendDepthStream();
		public abstract void ResumeDepthStream();

		protected void RaiseColorFrameReadyEvent(ImageData image)
		{
			ColorFrameReady?.Invoke(image);
		}

		protected void RaiseDepthFrameReadyEvent(DepthMap depthMap)
		{
			DepthFrameReady?.Invoke(depthMap);
		}
	}
}