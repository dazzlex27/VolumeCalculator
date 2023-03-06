using Primitives;
using Primitives.Logging;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace DeviceIntegration.FrameProviders
{
	public sealed class FrameStream<T> : IDisposable
	{
		public event Action<T> FrameReady;
		public event Action<T> UnrestrictedFrameReady;

		private readonly ILogger _logger;
		private readonly string _name;

		private readonly FixedSizeQueue<T> _frameQueue;
		private double _fps;
		private DateTime _lastProcessedFrameTime;
		private TimeSpan _timeBetweenFrames;

		public FrameStream(ILogger logger, string name, CancellationToken token)
		{
			_logger = logger;
			_name = name;

			_frameQueue = new FixedSizeQueue<T>(5);
			Fps = -1;
			_lastProcessedFrameTime = DateTime.MinValue;

			Task.Factory.StartNew(o => PollFrames(token), TaskCreationOptions.LongRunning, token);
		}

		public double Fps
		{
			get => _fps;
			set
			{
				_fps = value;

				if (_fps > 0)
				{
					_timeBetweenFrames = TimeSpan.FromMilliseconds(1000 / _fps);
					_logger.LogInfo($"FrameProvider: {_name} stream fps was set to {_fps}");
				}
				else
				{
					_timeBetweenFrames = TimeSpan.Zero;
					_logger.LogInfo($"FrameProvider: {_name} stream fps was reset");
				}
			}
		}

		public bool IsSuspended { get; set; }

		public bool NeedRestrictedFrame => IsSubscribedTo && !IsSuspended && TimeToProcessFrame;

		public bool NeedUnrestrictedFrame => UnrestrictedFrameReady?.GetInvocationList().Length > 0;

		public bool NeedAnyFrame => NeedRestrictedFrame || NeedUnrestrictedFrame;

		private bool IsSubscribedTo => FrameReady?.GetInvocationList().Length > 0;

		private bool TimeToProcessFrame => _lastProcessedFrameTime + _timeBetweenFrames < DateTime.Now;

		public void Dispose()
		{
			_frameQueue?.Dispose();
		}

		public void PushFrame(T frame)
		{
			_frameQueue.Enqueue(frame);
		}

		public void Suspend()
		{
			if (IsSuspended)
				return;

			IsSuspended = true;
		}

		public void Resume()
		{
			if (!IsSuspended)
				return;

			IsSuspended = false;
		}

		private async Task PollFrames(CancellationToken token)
		{
			try
			{
				while (!token.IsCancellationRequested)
				{
					if (IsSuspended)
					{
						await Task.Delay(10, token);
						continue;
					}

					var image = _frameQueue.Dequeue();
					if (NeedUnrestrictedFrame)
						UnrestrictedFrameReady?.Invoke(image);

					if (NeedRestrictedFrame)
					{
						_lastProcessedFrameTime = DateTime.Now;
						FrameReady?.Invoke(image);
					}
				}
			}
			catch (Exception ex)
			{
				_logger.LogException($"failed to handle {_name} frame", ex);
			}
		}
	}
}
