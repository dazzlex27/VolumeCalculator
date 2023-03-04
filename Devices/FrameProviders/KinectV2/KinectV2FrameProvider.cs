using Microsoft.Kinect;
using Primitives;
using Primitives.Logging;

namespace FrameProviders.KinectV2
{
	internal class KinectV2FrameProvider : FrameProvider
	{
		private readonly KinectSensor _kinectSensor;
		private readonly ColorFrameReader _colorFrameReader;
		private readonly DepthFrameReader _depthFrameReader;

		private readonly object _colorFrameProcessingLock;
		private readonly object _depthFrameProcessingLock;

		private bool _started;

		public KinectV2FrameProvider(ILogger logger)
			: base(logger)
		{
			_colorFrameProcessingLock = new object();
			_depthFrameProcessingLock = new object();

			Logger.LogInfo("Creating KinectV2 frame provider...");

			_kinectSensor = KinectSensor.GetDefault();

			_colorFrameReader = _kinectSensor.ColorFrameSource.OpenReader();
			_depthFrameReader = _kinectSensor.DepthFrameSource.OpenReader();

			_colorFrameReader.FrameArrived += OnColorFrameArrived;
			_depthFrameReader.FrameArrived += OnDepthFrameArrived;

			Logger.LogInfo("Created KinectV2 frame provider");
		}

		public override void Start()
		{
			if (_started)
				return;

			_started = true;
			
			Logger.LogInfo("Starting KinectV2 frame provider...");
			
			Paused = false;
			_kinectSensor.Open();
		}

		public override void Dispose()
		{
			Logger.LogInfo("Disposing KinectV2 frame provider...");
			TokenSource.Cancel();
			_colorFrameReader.Dispose();
			_depthFrameReader.Dispose();
			_kinectSensor.Close();
		}

		public override ColorCameraParams GetColorCameraParams()
		{
			// TODO: use calibration
			return new ColorCameraParams(84.1f, 53.8f, 1081.37f, 1081.37f, 959.5f, 539.5f);
		}

		public override DepthCameraParams GetDepthCameraParams()
		{
			return GetOfflineDepthCameraParams();
		}

		private static DepthCameraParams GetOfflineDepthCameraParams()
		{
			return new DepthCameraParams(70.6f, 60.0f, 367.7066f, 367.7066f, 257.8094f, 207.3965f, 600, 5000);
		}

		private void OnColorFrameArrived(object sender, ColorFrameArrivedEventArgs e)
		{
			lock (_colorFrameProcessingLock)
			{
				var needToProcess = NeedUnrestrictedColorFrame || NeedColorFrame;
				if (!needToProcess)
					return;

				ImageData image;

				using (var colorFrame = e.FrameReference.AcquireFrame())
				{
					image = KinectUtils.CreateColorFrameFromKinectFrame(colorFrame);
					if (image == null)
						return;
				}

				PushColorFrame(image);
			}
		}

		private void OnDepthFrameArrived(object sender, DepthFrameArrivedEventArgs e)
		{
			lock (_depthFrameProcessingLock)
			{
				var needToProcess = NeedUnrestrictedDepthFrame || NeedDepthFrame;
				if (!needToProcess)
					return;

				DepthMap map;

				using (var depthFrame = e.FrameReference.AcquireFrame())
				{
					map = KinectUtils.CreateDepthMapFromKinectFrame(depthFrame);
					if (map == null)
						return;
				}

				PushDepthFrame(map);
			}
		}
	}
}