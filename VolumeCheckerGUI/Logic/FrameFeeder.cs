using System;
using System.Runtime.InteropServices;
using VolumeCheckerGUI.Entities;
using VolumeCheckerGUI.Utils;

namespace VolumeCheckerGUI.Logic
{
	internal unsafe class FrameFeeder : IDisposable
	{
		private readonly DllWrapper.ColorFrameCallback _colorFrameCallback;
		private readonly DllWrapper.DepthFrameCallback _depthFramesCallback;
		private readonly Logger _logger;

		public event Action<ImageData> ColorFrameReady;
		public event Action<DepthMap> DepthFrameReady;

		public FrameFeeder(Logger logger)
		{
			_logger = logger;

			_logger.LogInfo("Creating frame feeder...");

			_colorFrameCallback = ColorFrameCallback;
			_depthFramesCallback = DepthFrameCallback;
		}

		public void Start()
		{
			_logger.LogInfo("Starting frame feeder...");
			DllWrapper.CreateFrameFeeder();
			DllWrapper.SubscribeToColorFrames(_colorFrameCallback);
			DllWrapper.SubscribeToDepthFrames(_depthFramesCallback);
		}

		public void Dispose()
		{
			_logger.LogInfo("Disposing frame feeder...");
			DllWrapper.UnsubscribeFromColorFrames(_colorFrameCallback);
			DllWrapper.UnsubscribeFromDepthFrames(_depthFramesCallback);
			DllWrapper.DestroyFrameFeeder();
		}

		private void ColorFrameCallback(ColorFrame* frame)
		{
			var dataLength = frame->Width * frame->Height * 3;

			var data = new byte[dataLength];
			Marshal.Copy(new IntPtr(frame->Data), data, 0, data.Length);

			var image = new ImageData(frame->Width, frame->Height, data);

			ColorFrameReady?.Invoke(image);
		}

		private void DepthFrameCallback(DepthFrame* frame)
		{
			var mapLength = frame->Width * frame->Height;
			var data = new short[mapLength];
			Marshal.Copy(new IntPtr(frame->Data), data, 0, data.Length);
			var depthMap = new DepthMap(frame->Width, frame->Height, data);

			DepthFrameReady?.Invoke(depthMap);
		}
	}
}