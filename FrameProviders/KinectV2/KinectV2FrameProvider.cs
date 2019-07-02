using System.Runtime.InteropServices;
using Microsoft.Kinect;
using Primitives;
using Primitives.Logging;
using ColorFrame = Microsoft.Kinect.ColorFrame;
using DepthFrame = Microsoft.Kinect.DepthFrame;

namespace FrameProviders.KinectV2
{
	internal class KinectV2FrameProvider : FrameProvider
	{
		private readonly ILogger _logger;
		private readonly KinectSensor _kinectSensor;
		private readonly ColorFrameReader _colorFrameReader;
		private readonly DepthFrameReader _depthFrameReader;

		private readonly object _colorFrameProcessingLock;
		private readonly object _depthFrameProcessingLock;

		public KinectV2FrameProvider(ILogger logger)
			: base(logger)
		{
			_colorFrameProcessingLock = new object();
			_depthFrameProcessingLock = new object();

			_logger = logger;
			_logger.LogInfo("Creating KinectV2 frame provider...");

			_kinectSensor = KinectSensor.GetDefault();

			_colorFrameReader = _kinectSensor.ColorFrameSource.OpenReader();
			_depthFrameReader = _kinectSensor.DepthFrameSource.OpenReader();

			_colorFrameReader.FrameArrived += ColorFrameReader_FrameArrived;
			_depthFrameReader.FrameArrived += DepthFrameReader_FrameArrived;

			IsColorStreamSuspended = false;
			IsColorStreamSuspended = false;
		}

		public override void Start()
		{
			_logger.LogInfo("Starting KinectV2 frame provider...");
			_kinectSensor.Open();
		}

		public override void Dispose()
		{
			_logger.LogInfo("Disposing KinectV2 frame provider...");

			_colorFrameReader.Dispose();
			_depthFrameReader.Dispose();
			_kinectSensor.Close();
		}

		public override void SuspendColorStream()
		{
			if (IsColorStreamSuspended)
				return;

			IsColorStreamSuspended = true;
		}

		public override void ResumeColorStream()
		{
			if (!IsColorStreamSuspended)
				return;

			IsColorStreamSuspended = false;
		}

		public override void SuspendDepthStream()
		{
			if (IsColorStreamSuspended)
				return;

			IsColorStreamSuspended = true;
		}

		public override void ResumeDepthStream()
		{
			if (!IsColorStreamSuspended)
				return;

			IsColorStreamSuspended = false;
		}

		public override ColorCameraParams GetColorCameraParams()
		{
			// TODO: use calibration
			return new ColorCameraParams(84.1f, 53.8f, 1081.37f, 1081.37f, 959.5f, 539.5f);
		}

		public override DepthCameraParams GetDepthCameraParams()
		{
			if (!_kinectSensor.IsAvailable)
				return GetOfflineDepthCameraParams();

			var frameSource = _kinectSensor.DepthFrameSource;
			var frameDescription = frameSource.FrameDescription;
			var intristics = _kinectSensor.CoordinateMapper.GetDepthCameraIntrinsics();
			return new DepthCameraParams(frameDescription.HorizontalFieldOfView, frameDescription.VerticalFieldOfView,
				intristics.FocalLengthX, intristics.FocalLengthY, intristics.PrincipalPointX, intristics.PrincipalPointY, 
				(short) frameSource.DepthMinReliableDistance, (short) frameSource.DepthMaxReliableDistance);
		}

		private static DepthCameraParams GetOfflineDepthCameraParams()
		{
			return new DepthCameraParams(70.6f, 60.0f, 367.7066f, 367.7066f, 257.8094f, 207.3965f, 600, 5000);
		}

		private void ColorFrameReader_FrameArrived(object sender, ColorFrameArrivedEventArgs e)
		{
			lock (_colorFrameProcessingLock)
			{
				var needToProcess = NeedUnrestrictedColorFrame || NeedColorFrame;
				if (!needToProcess)
					return;

				ImageData image;

				using (var colorFrame = e.FrameReference.AcquireFrame())
				{
					image = CreateColorFrameFromKinectFrame(colorFrame);
					if (image == null)
						return;
				}

				if (NeedUnrestrictedColorFrame)
					RaiseUnrestrictedColorFrameReadyEvent(image);

				if (NeedColorFrame)
					RaiseColorFrameReadyEvent(image);
			}
		}

		private void DepthFrameReader_FrameArrived(object sender, DepthFrameArrivedEventArgs e)
		{
			lock (_depthFrameProcessingLock)
			{
				var needToProcess = NeedUnrestrictedDepthFrame || NeedDepthFrame;
				if (!needToProcess)
					return;

				DepthMap map;

				using (var depthFrame = e.FrameReference.AcquireFrame())
				{
					map = CreateDepthMapFromKinectFrame(depthFrame);
					if (map == null)
						return;
				}

				if (NeedUnrestrictedDepthFrame)
					RaiseUnrestrictedDepthFrameReadyEvent(map);

				if (NeedDepthFrame)
					RaiseDepthFrameReadyEvent(map);
			}
		}

		private static ImageData CreateColorFrameFromKinectFrame(ColorFrame colorFrame)
		{
			if (colorFrame == null)
				return null;

			var frameDescription = colorFrame.FrameDescription;
			var frameLength = frameDescription.Width * frameDescription.Height;
			var data = new byte[frameLength * 4];

			using (var colorBuffer = colorFrame.LockRawImageBuffer())
			{
				var pinnedArray = GCHandle.Alloc(data, GCHandleType.Pinned);
				var pointer = pinnedArray.AddrOfPinnedObject();

				colorFrame.CopyConvertedFrameDataToIntPtr(pointer, (uint)(frameLength * 4),
					ColorImageFormat.Bgra);

				pinnedArray.Free();
			}

			return new ImageData(frameDescription.Width, frameDescription.Height, data, 4);
		}

		private static DepthMap CreateDepthMapFromKinectFrame(DepthFrame depthFrame)
		{
			if (depthFrame == null)
				return null;

			var frameDescription = depthFrame.FrameDescription;
			var frameLength = frameDescription.Width * frameDescription.Height;
			var data = new short[frameLength];

			using (var depthBuffer = depthFrame.LockImageBuffer())
			{
				Marshal.Copy(depthBuffer.UnderlyingBuffer, data, 0, frameLength);
			}

			return new DepthMap(frameDescription.Width, frameDescription.Height, data);
		}
	}
}