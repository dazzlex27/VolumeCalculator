using DepthMapProcessorGUI.Utils;
using System;
using VolumeCalculatorGUI.Entities;

namespace VolumeCalculatorGUI.Logic.FrameSources
{
	internal abstract class FrameSource : IDisposable
	{
		protected Logger Log;

		public event Action<ImageData> ColorFrameReady;
		public event Action<DepthMap> DepthFrameReady;

		public abstract FovDescription GetFovDescription();

		public FrameSource(Logger logger)
		{
			Log = logger;

			Log.LogInfo("Creating frame source...");
		}

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