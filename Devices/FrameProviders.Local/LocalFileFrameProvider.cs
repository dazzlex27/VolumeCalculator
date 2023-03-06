using System;
using System.Threading;
using System.Threading.Tasks;
using DeviceIntegration.FrameProviders;
using Primitives.Logging;

namespace FrameProviders.Local
{
	internal class LocalFileFrameProvider : FrameProvider
	{
		private bool _started;

		public LocalFileFrameProvider(ILogger logger)
			: base(logger)
		{
			Logger.LogInfo("Created local frame provider");
		}

		public override ColorCameraParams GetColorCameraParams()
		{
			// TODO: read from file
			return new ColorCameraParams(84.1f, 53.8f, 1081.37f, 1081.37f, 959.5f, 539.5f);
		}

		public override DepthCameraParams GetDepthCameraParams()
		{
			// TODO: read from file
			return new DepthCameraParams(70.6f, 60.0f, 367.7066f, 367.7066f, 257.8094f, 207.3965f, 600, 5000);
		}

		public override void Start()
		{
			if (_started)
				return;

			_started = true;
			Logger.LogInfo("Starting local frame provider...");

			Paused = false;
			Task.Factory.StartNew(o => PushColorFrames(TokenSource), TaskCreationOptions.LongRunning, TokenSource.Token);
			Task.Factory.StartNew(o => PushDepthFrames(TokenSource), TaskCreationOptions.LongRunning, TokenSource.Token);
		}

		public override void Dispose()
		{
			_started = false;
			TokenSource.Cancel();
			base.Dispose();
		}

		private async Task PushColorFrames(CancellationTokenSource tokenSource)
		{
			try
			{
				// TODO: implement cachless mode (prevent excessive memory consumption)
				var colorFrames = await LocalFrameProviderUtils.ReadImagesAsync();
				if (colorFrames == null)
				{
					Logger.LogError(@"Directory for color frames in local frame provider was not found!");
					return;
				}

				var colorFrameIndex = 0;

				while (!tokenSource.IsCancellationRequested)
				{
					if (ColorFrameStream.IsSuspended || colorFrames.Count <= 0)
					{
						await Task.Delay(5);
						continue;
					}

					if (!ColorFrameStream.NeedAnyFrame)
					{
						await Task.Delay(5);
						continue;
					}

					if (colorFrameIndex == colorFrames.Count)
						colorFrameIndex = 0;

					var image = colorFrames[colorFrameIndex++];
					ColorFrameStream.PushFrame(image);

					await Task.Delay(33); // ~30 FPS
				}
			}
			catch (Exception ex)
			{
				Logger.LogException("LocalFrameProvider: Failed to push color frames", ex);
			}
		}

		private async Task PushDepthFrames(CancellationTokenSource tokenSource)
		{
			try
			{
				var depthFrames = await LocalFrameProviderUtils.ReadDepthMapsAsync();
				if (depthFrames == null)
				{
					Logger.LogError(@"Directory for depth frames in local frame provider was not found!");
					return;
				}

				var depthFrameIndex = 0;

				while (!tokenSource.IsCancellationRequested)
				{
					if (DepthFrameStream.IsSuspended || depthFrames.Count <= 0)
					{
						await Task.Delay(5);
						continue;
					}

					if (!DepthFrameStream.NeedAnyFrame)
					{
						await Task.Delay(5);
						continue;
					}

					if (depthFrameIndex == depthFrames.Count)
						depthFrameIndex = 0;

					var depthMap = depthFrames[depthFrameIndex++];
					DepthFrameStream.PushFrame(depthMap);

					await Task.Delay(33); // ~30 FPS
				}
			}
			catch (Exception ex)
			{
				Logger.LogException("Error in local frame provider", ex);
			}
		}
	}
}
