using System.IO;
using DeviceIntegrations.Scales;
using FrameProviders;
using FrameProviders.D435;
using FrameProviders.KinectV2;
using FrameProviders.LocalFiles;
using Primitives.Logging;

namespace VolumeCalculatorGUI.Utils
{
	internal class DeviceInitializationUtils
	{
		public static FrameProvider CreateRequestedFrameProvider(ILogger logger)
		{
			if (Directory.Exists(Constants.LocalFrameProviderFolderName))
				return new LocalFileFrameProvider(logger);
			
			if (File.Exists(Constants.RealsenseFrameProviderFileName))
				return new RealsenseD435FrameProvider(logger);

			return new KinectV2FrameProvider(logger);
		}

		public static IScales CreateRequestedScales(ILogger logger, string scalesComPort)
		{
			if (File.Exists(Constants.FakeScalesFileName))
				return new FakeScales(logger);

			return new MassaKScales(logger, scalesComPort, Constants.ScalesPollingRateMs);
		}
	}
}