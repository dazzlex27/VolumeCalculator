using System;
using System.Threading;
using Primitives;
using Primitives.Logging;
using FrameProviders;

namespace DeviceIntegration.FrameProviders
{
	public abstract class FrameProvider : IFrameProvider
	{
		public event Action<ImageData> ColorFrameReady;
		public event Action<ImageData> UnrestrictedColorFrameReady;

		public event Action<DepthMap> DepthFrameReady;
		public event Action<DepthMap> UnrestrictedDepthFrameReady;

		protected readonly ILogger Logger;
		protected readonly CancellationTokenSource TokenSource;

		protected readonly FrameStream<ImageData> ColorFrameStream;
		protected readonly FrameStream<DepthMap> DepthFrameStream;

		protected FrameProvider(ILogger logger)
		{
			Logger = logger;
			TokenSource = new CancellationTokenSource();
			Paused = true;

			ColorFrameStream = new FrameStream<ImageData>(logger, "color", TokenSource.Token);
			ColorFrameStream.FrameReady += OnColorFrameReady;
			ColorFrameStream.UnrestrictedFrameReady += OnUnrestrictedColorFrameReady;

			DepthFrameStream = new FrameStream<DepthMap>(logger, "depth", TokenSource.Token);
			DepthFrameStream.FrameReady += OnDepthFrameReady;
			DepthFrameStream.UnrestrictedFrameReady += OnUnrestrictedDepthFrameReady;
		}

		private void OnUnrestrictedDepthFrameReady(DepthMap map)
		{
			UnrestrictedDepthFrameReady?.Invoke(map);
		}

		private void OnDepthFrameReady(DepthMap map)
		{
			DepthFrameReady?.Invoke(map);
		}

		private void OnUnrestrictedColorFrameReady(ImageData image)
		{
			UnrestrictedColorFrameReady?.Invoke(image);
		}

		private void OnColorFrameReady(ImageData image)
		{
			ColorFrameReady?.Invoke(image);
		}

		public double ColorCameraFps
		{
			get => ColorFrameStream.Fps;
			set => ColorFrameStream.Fps = value;
		}

		public double DepthCameraFps
		{
			get => DepthFrameStream.Fps;
			set => DepthFrameStream.Fps = value;
		}

		protected bool Paused { get; set; }

		public virtual void SuspendColorStream()
		{
			ColorFrameStream.Suspend();
		}

		public virtual void ResumeColorStream()
		{
			ColorFrameStream.Resume();
		}

		public virtual void SuspendDepthStream()
		{
			DepthFrameStream.Suspend();
		}

		public virtual void ResumeDepthStream()
		{
			DepthFrameStream.Resume();
		}

		public abstract ColorCameraParams GetColorCameraParams();

		public abstract DepthCameraParams GetDepthCameraParams();

		public abstract void Start();

		public virtual void Dispose()
		{
			TokenSource.Dispose();
			ColorFrameStream.Dispose();
			DepthFrameStream.Dispose();
		}
	}
}
