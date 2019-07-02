using System;
using FrameProviders.D435;
using FrameProviders.KinectV2;
using FrameProviders.LocalFiles;
using Primitives.Logging;

namespace FrameProviders
{
	public class FrameProvidersCommon
	{
		public static IFrameProvider CreateRequestedFrameProvider(string deviceName, ILogger logger)
		{
			switch (deviceName)
			{
				case "kinectv2":
					return new KinectV2FrameProvider(logger);
				case "d435":
					return new RealsenseD435FrameProvider(logger);
				case "local":
					return new LocalFileFrameProvider(logger);
				default:
					logger.LogError($"Failed to parse frame provider by the name \"{deviceName}\"");
					throw new NotSupportedException();
			}
		}
	}
}