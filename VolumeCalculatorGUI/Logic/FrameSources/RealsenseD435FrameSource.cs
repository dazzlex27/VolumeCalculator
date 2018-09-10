using System;
using System.Runtime.InteropServices;
using DepthMapProcessorGUI.Entities;
using DepthMapProcessorGUI.Utils;
using VolumeCalculatorGUI.Entities;

namespace VolumeCalculatorGUI.Logic.FrameSources
{
	internal unsafe class RealsenseD435FrameSource : FrameSource
	{
		private readonly DllWrapper.ColorFrameCallback _colorFrameCallback;
		private readonly DllWrapper.DepthFrameCallback _depthFramesCallback;

		private bool _colorStreamSuspended;
		private bool _depthStreamSuspended;

		public RealsenseD435FrameSource(Logger logger)
			: base(logger)
		{
			Log.LogInfo("Creating Realsense D435 frame receiver...");

			_colorFrameCallback = ColorFrameCallback;
			_depthFramesCallback = DepthFrameCallback;
		}

		public override void Start()
		{
			Log.LogInfo("Starting Realsense D435 frame receiver...");
			DllWrapper.CreateFrameFeeder();
			DllWrapper.SubscribeToColorFrames(_colorFrameCallback);
			DllWrapper.SubscribeToDepthFrames(_depthFramesCallback);
		}

		public override void Dispose()
		{
			Log.LogInfo("Disposing Realsense D435 frame receiver...");
			DllWrapper.UnsubscribeFromColorFrames(_colorFrameCallback);
			DllWrapper.UnsubscribeFromDepthFrames(_depthFramesCallback);
			DllWrapper.DestroyFrameFeeder();
		}

		public override FovDescription GetFovDescription()
		{
			return new FovDescription(91, 57);
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

		private void ColorFrameCallback(ColorFrame* frame)
		{
			var dataLength = frame->Width * frame->Height * 3;

			var data = new byte[dataLength];
			Marshal.Copy(new IntPtr(frame->Data), data, 0, data.Length);

			var image = new ImageData(frame->Width, frame->Height, data, 3);

			RaiseColorFrameReadyEvent(image);
		}

		private void DepthFrameCallback(DepthFrame* frame)
		{
			var mapLength = frame->Width * frame->Height;
			var data = new short[mapLength];
			Marshal.Copy(new IntPtr(frame->Data), data, 0, data.Length);
			var depthMap = new DepthMap(frame->Width, frame->Height, data);

			RaiseDepthFrameReadyEvent(depthMap);
		}
	}
}