using System;
using System.Runtime.InteropServices;
using Primitives;
using Primitives.Logging;

namespace FrameProviders.D435
{
	public class RealsenseD435FrameProvider : FrameProvider
	{
		private readonly ILogger _logger;
		private readonly DllWrapper.ColorFrameCallback _colorFrameCallback;
		private readonly DllWrapper.DepthFrameCallback _depthFramesCallback;

		private readonly object _colorFrameProcessingLock;
		private readonly object _depthFrameProcessingLock;

		public RealsenseD435FrameProvider(ILogger logger)
			: base(logger)
		{
			_colorFrameProcessingLock = new object();
			_depthFrameProcessingLock = new object();

			_logger = logger;
			_logger.LogInfo("Creating Realsense D435 frame receiver...");

			unsafe
			{
				_colorFrameCallback = ColorFrameCallback;
				_depthFramesCallback = DepthFrameCallback;
			}
		}

		public override void Start()
		{
			_logger.LogInfo("Starting Realsense D435 frame receiver...");
			DllWrapper.CreateFrameProvider();
			DllWrapper.SubscribeToColorFrames(_colorFrameCallback);
			DllWrapper.SubscribeToDepthFrames(_depthFramesCallback);
		}

		public override void Dispose()
		{
			_logger.LogInfo("Disposing Realsense D435 frame receiver...");
			DllWrapper.UnsubscribeFromColorFrames(_colorFrameCallback);
			DllWrapper.UnsubscribeFromDepthFrames(_depthFramesCallback);
			DllWrapper.DestroyFrameProvider();
		}

		public override ColorCameraParams GetColorCameraParams()
		{
			return new ColorCameraParams(69.4f, 42.5f, 1376.13f, 1376.61f, 956.491f, 544.128f);
		}

		public override DepthCameraParams GetDepthCameraParams()
		{
			//var intristics = DllWrapper.GetDepthCameraIntrinsics();
			//if (intristics == null)
				return new DepthCameraParams(86, 57, 645.715759f, 645.715759f, 637.416138f, 362.886597f, 300, 10000);

			//return new DepthCameraParams(86, 57, intristics.FocalLengthX, intristics.FocalLengthY,
			//	intristics.PrincipalPointX, intristics.PrincipalPointY, 300, 10000);
		}

		public override void SuspendColorStream()
		{
			if (IsColorStreamSuspended)
				return;

			DllWrapper.UnsubscribeFromColorFrames(_colorFrameCallback);

			IsColorStreamSuspended = true;
		}

		public override void ResumeColorStream()
		{
			if (!IsColorStreamSuspended)
				return;

			DllWrapper.SubscribeToColorFrames(_colorFrameCallback);

			IsColorStreamSuspended = false;
		}

		public override void SuspendDepthStream()
		{
			if (IsColorStreamSuspended)
				return;

			DllWrapper.UnsubscribeFromDepthFrames(_depthFramesCallback);

			IsColorStreamSuspended = true;
		}

		public override void ResumeDepthStream()
		{
			if (!IsColorStreamSuspended)
				return;

			DllWrapper.SubscribeToDepthFrames(_depthFramesCallback);

			IsColorStreamSuspended = false;
		}

		private unsafe void ColorFrameCallback(ColorFrame* frame)
		{
			if (frame == null)
				return;

			lock (_colorFrameProcessingLock)
			{
				var needToProcess = NeedUnrestrictedColorFrame || NeedColorFrame;
				if (!needToProcess)
					return;

				try
				{
					var dataLength = frame->Width * frame->Height * 3;

					var data = new byte[dataLength];
					Marshal.Copy(new IntPtr(frame->Data), data, 0, data.Length);

					var image = new ImageData(frame->Width, frame->Height, data, 3);

					if (NeedUnrestrictedColorFrame)
						RaiseUnrestrictedColorFrameReadyEvent(image);

					if (NeedColorFrame)
						RaiseColorFrameReadyEvent(image);
				}
				catch (Exception ex)
				{
					_logger.LogException("Failed to receive a color frame", ex);
				}
			}
		}

		private unsafe void DepthFrameCallback(DepthFrame* frame)
		{
			if (frame == null)
				return;

			lock (_depthFrameProcessingLock)
			{
				var needToProcess = NeedUnrestrictedDepthFrame || NeedDepthFrame;
				if (!needToProcess)
					return;

				try
				{
					var mapLength = frame->Width * frame->Height;
					var data = new short[mapLength];
					Marshal.Copy(new IntPtr(frame->Data), data, 0, data.Length);
					var depthMap = new DepthMap(frame->Width, frame->Height, data);

					if (NeedUnrestrictedDepthFrame)
						RaiseUnrestrictedDepthFrameReadyEvent(depthMap);

					if (NeedDepthFrame)
						RaiseDepthFrameReadyEvent(depthMap);
				}
				catch (Exception ex)
				{
					_logger.LogException("Failed to receive a depth frame", ex);
				}
			}
		}
	}
}