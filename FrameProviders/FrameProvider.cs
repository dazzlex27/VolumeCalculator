using System;
using Primitives;
using Primitives.Logging;

namespace FrameProviders
{
	public abstract class FrameProvider : IDisposable
	{
		public event Action<ImageData> ColorFrameReady;
		public event Action<DepthMap> DepthFrameReady;

		public event Action<ImageData> UnrestrictedColorFrameReady;
		public event Action<DepthMap> UnrestrictedDepthFrameReady;

		private readonly ILogger _logger;

		private double _colorCameraFps;
		private double _depthCameraFps;

		public  double ColorCameraFps
		{
			get => _colorCameraFps;
			set
			{
				_colorCameraFps = value;

				if (_colorCameraFps > 0)
				{
					_timeBetweenColorFrames = TimeSpan.FromMilliseconds(1000 / _colorCameraFps);
					_logger.LogInfo($"FrameProvider: color camera fps was set to {_colorCameraFps}");
				}
				else
				{
					_timeBetweenColorFrames = TimeSpan.Zero;
					_logger.LogInfo("FrameProvider: color camera fps was reset");
				}
			}
		}

		public double DepthCameraFps
		{
			get => _depthCameraFps;
			set
			{
				_depthCameraFps = value;

				if (_depthCameraFps > 0)
				{
					_timeBetweenDepthFrames = TimeSpan.FromMilliseconds(1000 / _depthCameraFps);
					_logger.LogInfo($"FrameProvider: depth camera fps was set to {_depthCameraFps}");
				}
				else
				{
					_timeBetweenDepthFrames = TimeSpan.Zero;
					_logger.LogInfo("FrameProvider: depth camera fps was reset");
				}
			}
		}

		private TimeSpan _timeBetweenColorFrames;
		private TimeSpan _timeBetweenDepthFrames;

		protected virtual bool NeedUnrestrictedColorFrame => IsUnrestrictedColorStreamSubsribedTo;
		protected virtual bool NeedColorFrame => IsColorStreamSubsribedTo && !IsColorStreamSuspended && TimeToProcessColorFrame;

		protected virtual bool NeedUnrestrictedDepthFrame => IsUnrestrictedDepthStreamSubsribedTo;
		protected virtual bool NeedDepthFrame => IsDepthStreamSubsribedTo && !IsDepthStreamSuspended && TimeToProcessDepthFrame;

		protected bool IsColorStreamSuspended { get; set; }
		protected bool IsDepthStreamSuspended { get; set; }

		protected bool IsUnrestrictedColorStreamSubsribedTo => UnrestrictedColorFrameReady?.GetInvocationList().Length > 0;
		protected bool IsUnrestrictedDepthStreamSubsribedTo => UnrestrictedDepthFrameReady?.GetInvocationList().Length > 0;

		protected bool IsColorStreamSubsribedTo => ColorFrameReady?.GetInvocationList().Length > 0;
		protected bool IsDepthStreamSubsribedTo => DepthFrameReady?.GetInvocationList().Length > 0;

		private DateTime _lastProcessedColorFrameTime;
		private DateTime _lastProcessedDepthFrameTime;

		protected bool TimeToProcessColorFrame => _lastProcessedColorFrameTime + _timeBetweenColorFrames < DateTime.Now;
		protected bool TimeToProcessDepthFrame => _lastProcessedDepthFrameTime + _timeBetweenDepthFrames < DateTime.Now;

		public abstract ColorCameraParams GetColorCameraParams();
		public abstract DepthCameraParams GetDepthCameraParams();

		protected FrameProvider(ILogger logger)
		{
			_logger = logger;

			ColorCameraFps = -1;
			DepthCameraFps = -1;

			_lastProcessedColorFrameTime = DateTime.MinValue;
			_lastProcessedDepthFrameTime = DateTime.MinValue;
		}

		public abstract void Start();
		public abstract void Dispose();

		public abstract void SuspendColorStream();
		public abstract void ResumeColorStream();

		public abstract void SuspendDepthStream();
		public abstract void ResumeDepthStream();

		protected void RaiseUnrestrictedColorFrameReadyEvent(ImageData image)
		{
			UnrestrictedColorFrameReady?.Invoke(image);
		}

		protected void RaiseUnrestrictedDepthFrameReadyEvent(DepthMap depthMap)
		{
			UnrestrictedDepthFrameReady?.Invoke(depthMap);
		}

		protected void RaiseColorFrameReadyEvent(ImageData image)
		{
			_lastProcessedColorFrameTime = DateTime.Now;
			ColorFrameReady?.Invoke(image);
		}

		protected void RaiseDepthFrameReadyEvent(DepthMap depthMap)
		{
			_lastProcessedDepthFrameTime = DateTime.Now;
			DepthFrameReady?.Invoke(depthMap);
		}
	}
}