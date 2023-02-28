using System;
using System.Runtime.InteropServices;
using Primitives;
using Primitives.Logging;

namespace FrameProviders.D435
{
	internal class RealsenseD435FrameProvider : FrameProvider
	{
		private readonly NativeMethods.ColorFrameCallback _colorFrameCallback;
		private readonly NativeMethods.DepthFrameCallback _depthFramesCallback;

		private readonly object _colorFrameProcessingLock;
		private readonly object _depthFrameProcessingLock;

		public RealsenseD435FrameProvider(ILogger logger)
			: base(logger)
		{
			_colorFrameProcessingLock = new object();
			_depthFrameProcessingLock = new object();
			
			Logger.LogInfo("Creating Realsense D435 frame receiver...");

			unsafe
			{
				_colorFrameCallback = ColorFrameCallback;
				_depthFramesCallback = DepthFrameCallback;
			}
		}

		public override void Start()
		{
			Logger.LogInfo("Starting Realsense D435 frame receiver...");
			NativeMethods.CreateFrameProvider();
			NativeMethods.SubscribeToColorFrames(_colorFrameCallback);
			NativeMethods.SubscribeToDepthFrames(_depthFramesCallback);
		}

		public override void Dispose()
		{
			Logger.LogInfo("Disposing Realsense D435 frame receiver...");
			NativeMethods.UnsubscribeFromColorFrames(_colorFrameCallback);
			NativeMethods.UnsubscribeFromDepthFrames(_depthFramesCallback);
			NativeMethods.DestroyFrameProvider();
		}

		public override ColorCameraParams GetColorCameraParams()
		{
			return new ColorCameraParams(69.4f, 42.5f, 1376.13f, 1376.61f, 956.491f, 544.128f);
		}

		public override DepthCameraParams GetDepthCameraParams()
		{
			//var intristics = NativeMethods.GetDepthCameraIntrinsics();
			//if (intristics == null)
				return new DepthCameraParams(86, 57, 645.715759f, 645.715759f, 637.416138f, 362.886597f, 300, 10000);

			//return new DepthCameraParams(86, 57, intristics.FocalLengthX, intristics.FocalLengthY,
			//	intristics.PrincipalPointX, intristics.PrincipalPointY, 300, 10000);
		}

		public override void SuspendColorStream()
		{
			if (IsColorStreamSuspended)
				return;

			NativeMethods.UnsubscribeFromColorFrames(_colorFrameCallback);

			IsColorStreamSuspended = true;
		}

		public override void ResumeColorStream()
		{
			if (!IsColorStreamSuspended)
				return;

			NativeMethods.SubscribeToColorFrames(_colorFrameCallback);

			IsColorStreamSuspended = false;
		}

		public override void SuspendDepthStream()
		{
			if (IsDepthStreamSuspended)
				return;

			NativeMethods.UnsubscribeFromDepthFrames(_depthFramesCallback);

			IsDepthStreamSuspended = true;
		}

		public override void ResumeDepthStream()
		{
			if (!IsDepthStreamSuspended)
				return;

			NativeMethods.SubscribeToDepthFrames(_depthFramesCallback);

			IsDepthStreamSuspended = false;
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

					PushColorFrame(image);
				}
				catch (Exception ex)
				{
					Logger.LogException("Failed to receive a color frame", ex);
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

					PushDepthFrame(depthMap);
				}
				catch (Exception ex)
				{
					Logger.LogException("Failed to receive a depth frame", ex);
				}
			}
		}
	}
}