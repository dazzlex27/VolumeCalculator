using System;
using System.Runtime.InteropServices;
using Common;

namespace FrameSources.D435
{
	public class RealsenseD435FrameSource : FrameSource
	{
		private readonly Logger _logger;
		private readonly DllWrapper.ColorFrameCallback _colorFrameCallback;
		private readonly DllWrapper.DepthFrameCallback _depthFramesCallback;

		private bool _colorStreamSuspended;
		private bool _depthStreamSuspended;

		public unsafe RealsenseD435FrameSource(Logger logger)
		{
			_logger = logger;
			_logger.LogInfo("Creating Realsense D435 frame receiver...");

			_colorFrameCallback = ColorFrameCallback;
			_depthFramesCallback = DepthFrameCallback;
		}

		public override void Start()
		{
			_logger.LogInfo("Starting Realsense D435 frame receiver...");
			DllWrapper.CreateFrameFeeder();
			DllWrapper.SubscribeToColorFrames(_colorFrameCallback);
			DllWrapper.SubscribeToDepthFrames(_depthFramesCallback);
		}

		public override void Dispose()
		{
			_logger.LogInfo("Disposing Realsense D435 frame receiver...");
			DllWrapper.UnsubscribeFromColorFrames(_colorFrameCallback);
			DllWrapper.UnsubscribeFromDepthFrames(_depthFramesCallback);
			DllWrapper.DestroyFrameFeeder();
		}

		public override DeviceParams GetDeviceParams()
		{
			return new DeviceParams(86, 57, -1, -1, 300, 10000);
		}

		public override void SuspendColorStream()
		{
			if (_colorStreamSuspended)
				return;

			DllWrapper.UnsubscribeFromColorFrames(_colorFrameCallback);

			_colorStreamSuspended = true;
		}

		public override void ResumeColorStream()
		{
			if (!_colorStreamSuspended)
				return;

			DllWrapper.SubscribeToColorFrames(_colorFrameCallback);

			_colorStreamSuspended = false;
		}

		public override void SuspendDepthStream()
		{
			if (_colorStreamSuspended)
				return;

			DllWrapper.UnsubscribeFromDepthFrames(_depthFramesCallback);

			_colorStreamSuspended = true;
		}

		public override void ResumeDepthStream()
		{
			if (!_depthStreamSuspended)
				return;

			DllWrapper.SubscribeToDepthFrames(_depthFramesCallback);

			_depthStreamSuspended = false;
		}

		private unsafe void ColorFrameCallback(ColorFrame* frame)
		{
			var dataLength = frame->Width * frame->Height * 3;

			var data = new byte[dataLength];
			Marshal.Copy(new IntPtr(frame->Data), data, 0, data.Length);

			var image = new ImageData(frame->Width, frame->Height, data, 3);

			RaiseColorFrameReadyEvent(image);
		}

		private unsafe void DepthFrameCallback(DepthFrame* frame)
		{
			var mapLength = frame->Width * frame->Height;
			var data = new short[mapLength];
			Marshal.Copy(new IntPtr(frame->Data), data, 0, data.Length);
			var depthMap = new DepthMap(frame->Width, frame->Height, data);

			RaiseDepthFrameReadyEvent(depthMap);
		}
	}
}