﻿using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Common;

namespace FrameProviders.LocalFiles
{
	public class LocalFileFrameProvider : FrameProvider
	{
		private static readonly string ColorFramesPath = Path.Combine("localFrameProvider", "color");
		private static readonly string DepthFramesPath = Path.Combine("localFrameProvider", "depth");

		private readonly ILogger _logger;

		private CancellationTokenSource _tokenSource;
		private bool _started;

		private bool _sendColorFrames;
		private bool _sendDepthFrames;

		public LocalFileFrameProvider(ILogger logger)
		{
			_logger = logger;
		}

		public override DeviceParams GetDeviceParams()
		{
			return new DeviceParams(70.6f, 60.0f, 367.7066f, 367.7066f, 257.8094f, 207.3965f, 600, 5000);
		}

		public override void Start()
		{
			if (_started)
				return;

			_started = true;

			_sendColorFrames = true;
			_sendDepthFrames = true;
			_tokenSource = new CancellationTokenSource();
			Task.Factory.StartNew(o => RunFrameGeneration(_tokenSource), TaskCreationOptions.LongRunning, _tokenSource.Token);
		}

		public override void Dispose()
		{
			_started = false;
			_tokenSource.Cancel();
		}

		public override void SuspendColorStream()
		{
			_sendColorFrames = false;
		}

		public override void ResumeColorStream()
		{
			_sendColorFrames = true;
		}

		public override void SuspendDepthStream()
		{
			_sendDepthFrames = false;
		}

		public override void ResumeDepthStream()
		{
			_sendDepthFrames = true;
		}

		private async Task RunFrameGeneration(CancellationTokenSource tokenSource)
		{
			if (!Directory.Exists(ColorFramesPath))
			{
				_logger.LogError($@"Directory {DepthFramesPath} for color frames in local frame provider was not found!");
				return;
			}

			if (!Directory.Exists(DepthFramesPath))
			{
				_logger.LogError($@"Directory {DepthFramesPath} for depth frames in local frame provider was not found!");
				return;
			}

			try
			{
				var colorFrames = new DirectoryInfo(ColorFramesPath).EnumerateFiles().ToArray();
				var depthFrames = new DirectoryInfo(DepthFramesPath).EnumerateFiles().ToArray();

				var colorFrameIndex = 0;
				var depthFrameIndex = 0;

				while (!tokenSource.IsCancellationRequested)
				{
					if (_sendColorFrames && colorFrames.Length > 0)
					{
						if (colorFrameIndex == colorFrames.Length)
							colorFrameIndex = 0;

						var nextColorFrameInfo = colorFrames[colorFrameIndex++];
						var image = ImageUtils.ReadImageDataFromFile(nextColorFrameInfo.FullName);
						RaiseColorFrameReadyEvent(image);
					}

					if (_sendDepthFrames && depthFrames.Length > 0)
					{
						if (depthFrameIndex == depthFrames.Length)
							depthFrameIndex = 0;

						var nextDepthFrameInfo = depthFrames[depthFrameIndex++];
						var depthMap = DepthMapUtils.ReadDepthMapFromRawFile(nextDepthFrameInfo.FullName);
						RaiseDepthFrameReadyEvent(depthMap);
					}

					await Task.Delay(100);
				}
			}
			catch (Exception ex)
			{
				_logger.LogException("Error in local frame provider", ex);
			}
		}
	}
}