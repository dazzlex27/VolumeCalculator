﻿using System;
using System.Runtime.InteropServices;
using Common;
using Microsoft.Kinect;

namespace FrameProviders.KinectV2
{
	public class KinectV2FrameProvider : FrameProvider
	{
		private readonly ILogger _logger;
		private readonly KinectSensor _kinectSensor;
		private readonly ColorFrameReader _colorFrameReader;
		private readonly DepthFrameReader _depthFrameReader;

		private bool _colorStreamSuspended;
		private bool _depthStreamSuspended;

		public KinectV2FrameProvider(ILogger logger)
		{
			_logger = logger;
			_logger.LogInfo("Creating KinectV2 frame receiver...");

			_kinectSensor = KinectSensor.GetDefault();

			_colorFrameReader = _kinectSensor.ColorFrameSource.OpenReader();
			_depthFrameReader = _kinectSensor.DepthFrameSource.OpenReader();

			_colorFrameReader.FrameArrived += ColorFrameReader_FrameArrived;
			_depthFrameReader.FrameArrived += DepthFrameReader_FrameArrived;

			_colorStreamSuspended = false;
			_depthStreamSuspended = false;
		}

		public override void Start()
		{
			_logger.LogInfo("Starting KinectV2 frame receiver...");
			_kinectSensor.Open();
		}

		public override void Dispose()
		{
			_logger.LogInfo("Disposing KinectV2 frame receiver...");

			_colorFrameReader.Dispose();
			_depthFrameReader.Dispose();
			_kinectSensor.Close();
		}

		public override void SuspendColorStream()
		{
			if (_colorStreamSuspended)
				return;

			_colorStreamSuspended = true;
		}

		public override void ResumeColorStream()
		{
			if (!_colorStreamSuspended)
				return;

			_colorStreamSuspended = false;
		}

		public override void SuspendDepthStream()
		{
			if (_depthStreamSuspended)
				return;

			_depthStreamSuspended = true;
		}

		public override void ResumeDepthStream()
		{
			if (!_depthStreamSuspended)
				return;

			_depthStreamSuspended = false;
		}

		public override DeviceParams GetDeviceParams()
		{
			if (!_kinectSensor.IsAvailable)
				return GetOfflineParams();

			var frameSource = _kinectSensor.DepthFrameSource;
			var frameDescription = frameSource.FrameDescription;
			var intristics = _kinectSensor.CoordinateMapper.GetDepthCameraIntrinsics();
			return new DeviceParams(frameDescription.HorizontalFieldOfView, frameDescription.VerticalFieldOfView,
				intristics.FocalLengthX, intristics.FocalLengthY, intristics.PrincipalPointX,
				intristics.PrincipalPointY, (short) frameSource.DepthMinReliableDistance,
				(short) frameSource.DepthMaxReliableDistance);
		}

		private static DeviceParams GetOfflineParams()
		{
			return new DeviceParams(70.6f, 60.0f, 367.7066f, 367.7066f, 257.8094f, 207.3965f, 600, 5000);
		}

		private void ColorFrameReader_FrameArrived(object sender, ColorFrameArrivedEventArgs e)
		{
			if (_colorStreamSuspended)
				return;

			using (var colorFrame = e.FrameReference.AcquireFrame())
			{
				if (colorFrame == null)
					return;

				var frameDescription = colorFrame.FrameDescription;
				var frameLength = frameDescription.Width * frameDescription.Height;
				var data = new byte[frameLength * 4];

				using (KinectBuffer colorBuffer = colorFrame.LockRawImageBuffer())
				{
					GCHandle pinnedArray = GCHandle.Alloc(data, GCHandleType.Pinned);
					IntPtr pointer = pinnedArray.AddrOfPinnedObject();

					colorFrame.CopyConvertedFrameDataToIntPtr(pointer, (uint)(frameLength * 4),
						ColorImageFormat.Bgra);

					pinnedArray.Free();
				}

				var image = new ImageData(frameDescription.Width, frameDescription.Height, data, 4);
				RaiseColorFrameReadyEvent(image);
			}
		}

		private void DepthFrameReader_FrameArrived(object sender, DepthFrameArrivedEventArgs e)
		{
			if (_depthStreamSuspended)
				return;

			using (var depthFrame = e.FrameReference.AcquireFrame())
			{
				if (depthFrame == null)
					return;

				var frameDescription = depthFrame.FrameDescription;
				var frameLength = frameDescription.Width * frameDescription.Height;
				var data = new short[frameLength];

				using (KinectBuffer depthBuffer = depthFrame.LockImageBuffer())
				{
					Marshal.Copy(depthBuffer.UnderlyingBuffer, data, 0, frameLength);
				}

				var map = new DepthMap(frameDescription.Width, frameDescription.Height, data);
				RaiseDepthFrameReadyEvent(map);
			}
		}
	}
}