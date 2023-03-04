using System;
using System.Threading;
using System.Threading.Tasks;
using Primitives;
using Primitives.Logging;
using FrameProviders;

namespace DeviceIntegration.FrameProviders
{
    public abstract class FrameProvider : IFrameProvider
    {
        public event Action<ImageData> ColorFrameReady;
        public event Action<DepthMap> DepthFrameReady;
        public event Action<ImageData> UnrestrictedColorFrameReady;
        public event Action<DepthMap> UnrestrictedDepthFrameReady;

        protected readonly CancellationTokenSource TokenSource;
        protected readonly ILogger Logger;

        private double _colorCameraFps;
        private double _depthCameraFps;

        private DateTime _lastProcessedColorFrameTime;
        private DateTime _lastProcessedDepthFrameTime;

        private TimeSpan _timeBetweenColorFrames;
        private TimeSpan _timeBetweenDepthFrames;

        private readonly FixedSizeQueue<ImageData> _imageQueue;
        private readonly FixedSizeQueue<DepthMap> _depthMapQueue;

        protected bool Paused { get; set; }

        public double ColorCameraFps
        {
            get => _colorCameraFps;
            set
            {
                _colorCameraFps = value;

                if (_colorCameraFps > 0)
                {
                    _timeBetweenColorFrames = TimeSpan.FromMilliseconds(1000 / _colorCameraFps);
                    Logger.LogInfo($"FrameProvider: color camera fps was set to {_colorCameraFps}");
                }
                else
                {
                    _timeBetweenColorFrames = TimeSpan.Zero;
                    Logger.LogInfo("FrameProvider: color camera fps was reset");
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
                    Logger.LogInfo($"FrameProvider: depth camera fps was set to {_depthCameraFps}");
                }
                else
                {
                    _timeBetweenDepthFrames = TimeSpan.Zero;
                    Logger.LogInfo("FrameProvider: depth camera fps was reset");
                }
            }
        }

        protected bool NeedUnrestrictedColorFrame => UnrestrictedColorFrameReady?.GetInvocationList().Length > 0;

        protected bool NeedColorFrame => IsColorStreamSubscribedTo && !IsColorStreamSuspended && TimeToProcessColorFrame;

        protected bool NeedUnrestrictedDepthFrame => UnrestrictedDepthFrameReady?.GetInvocationList().Length > 0;

        protected bool NeedDepthFrame => IsDepthStreamSubscribedTo && !IsDepthStreamSuspended && TimeToProcessDepthFrame;

        protected bool IsColorStreamSuspended { get; set; }

        protected bool IsDepthStreamSuspended { get; set; }

        private bool IsColorStreamSubscribedTo => ColorFrameReady?.GetInvocationList().Length > 0;

        private bool IsDepthStreamSubscribedTo => DepthFrameReady?.GetInvocationList().Length > 0;

        private bool TimeToProcessColorFrame => _lastProcessedColorFrameTime + _timeBetweenColorFrames < DateTime.Now;

        private bool TimeToProcessDepthFrame => _lastProcessedDepthFrameTime + _timeBetweenDepthFrames < DateTime.Now;

        protected FrameProvider(ILogger logger)
        {
            Logger = logger;

            ColorCameraFps = -1;
            DepthCameraFps = -1;

            _lastProcessedColorFrameTime = DateTime.MinValue;
            _lastProcessedDepthFrameTime = DateTime.MinValue;

            _imageQueue = new FixedSizeQueue<ImageData>(5);
            _depthMapQueue = new FixedSizeQueue<DepthMap>(5);

            Paused = true;

            TokenSource = new CancellationTokenSource();

            Task.Factory.StartNew(o => PollColorFrames(TokenSource), TaskCreationOptions.LongRunning, TokenSource.Token);
            Task.Factory.StartNew(o => PollDepthFrames(TokenSource), TaskCreationOptions.LongRunning, TokenSource.Token);
        }


        public virtual void SuspendColorStream()
        {
            if (IsColorStreamSuspended)
                return;

            IsColorStreamSuspended = true;
        }

        public virtual void ResumeColorStream()
        {
            if (!IsColorStreamSuspended)
                return;

            IsColorStreamSuspended = false;
        }

        public virtual void SuspendDepthStream()
        {
            if (IsDepthStreamSuspended)
                return;

            IsDepthStreamSuspended = true;
        }

        public virtual void ResumeDepthStream()
        {
            if (!IsDepthStreamSuspended)
                return;

            IsDepthStreamSuspended = false;
        }

        public abstract ColorCameraParams GetColorCameraParams();

        public abstract DepthCameraParams GetDepthCameraParams();

        public abstract void Start();

        public abstract void Dispose();

        protected void PushColorFrame(ImageData image)
        {
            _imageQueue.Enqueue(image);
        }

        protected void PushDepthFrame(DepthMap map)
        {
            _depthMapQueue.Enqueue(map);
        }

        private void RaiseUnrestrictedColorFrameReadyEvent(ImageData image)
        {
            UnrestrictedColorFrameReady?.Invoke(image);
        }

        private void RaiseUnrestrictedDepthFrameReadyEvent(DepthMap depthMap)
        {
            UnrestrictedDepthFrameReady?.Invoke(depthMap);
        }

        private void RaiseColorFrameReadyEvent(ImageData image)
        {
            _lastProcessedColorFrameTime = DateTime.Now;
            ColorFrameReady?.Invoke(image);
        }

        private void RaiseDepthFrameReadyEvent(DepthMap depthMap)
        {
            _lastProcessedDepthFrameTime = DateTime.Now;
            DepthFrameReady?.Invoke(depthMap);
        }

        private async Task PollColorFrames(CancellationTokenSource tokenSource)
        {
            try
            {
                while (!tokenSource.IsCancellationRequested)
                {
                    if (Paused)
                    {
                        await Task.Delay(10);
                        continue;
                    }

                    var image = _imageQueue.Dequeue();
                    if (image != null)
                    {
                        if (NeedUnrestrictedColorFrame)
                            RaiseUnrestrictedColorFrameReadyEvent(image);

                        if (NeedColorFrame)
                            RaiseColorFrameReadyEvent(image);
                    }
                    else
                        await Task.Delay(5);
                }
            }
            catch (Exception ex)
            {
                Logger.LogException("failed to handle color frame", ex);
            }
        }

        private async Task PollDepthFrames(CancellationTokenSource tokenSource)
        {
            try
            {
                while (!tokenSource.IsCancellationRequested)
                {
                    if (Paused)
                    {
                        await Task.Delay(10);
                        continue;
                    }

                    var depthMap = _depthMapQueue.Dequeue();
                    if (depthMap != null)
                    {
                        if (NeedUnrestrictedDepthFrame)
                            RaiseUnrestrictedDepthFrameReadyEvent(depthMap);

                        if (NeedDepthFrame)
                            RaiseDepthFrameReadyEvent(depthMap);
                    }
                    else
                        await Task.Delay(5);
                }
            }
            catch (Exception ex)
            {
                Logger.LogException("failed to handle depth frame", ex);
            }
        }
    }
}