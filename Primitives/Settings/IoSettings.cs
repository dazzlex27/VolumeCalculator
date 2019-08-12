using System.IO;
using System.Runtime.Serialization;

namespace Primitives.Settings
{
	public class IoSettings
	{
		public string ActiveCameraName { get; set; }

		public string ActiveScalesName { get; set; }

		public string ScalesPort { get; set; }

        public int ScalesMinWeight { get; set; }

		public IoEntry[] ActiveScanners { get; set; }

		public string ActiveIoCircuitName { get; set; }

		public string IoCircuitPort { get; set; }

		public string ActiveRangeMeterName { get; set; }

		public string RangeMeterPort { get; set; }

		public int RangeMeterSubtractionValueMm { get; set; }

		public IpCameraSettings IpCameraSettings { get; set; }

		public string OutputPath { get; set; }

		public bool ShutDownPcByDefault { get; set; }

		public string ResultsFilePath => Path.Combine(OutputPath, GlobalConstants.ResultsFileName);

		public string PhotosDirectoryPath => Path.Combine(OutputPath, GlobalConstants.ResultPhotosFolder);

		public IoSettings(string activeCameraName, string activeScalesName, string scalesPort, int scalesMinWeight, 
            IoEntry[] activeScanners, string activeIoCircuitName, string ioCircuitPort, string activeRangeMeterName, 
            string rangeMeterPort, int rangeMeterSubtractionValueMm, IpCameraSettings ipCameraSettings, string outputPath, 
            bool shutDownPcByDefault)
		{
			ActiveCameraName = activeCameraName;
			ActiveScalesName = activeScalesName;
			ScalesPort = scalesPort;
            ScalesMinWeight = scalesMinWeight;
			ActiveScanners = activeScanners;
			ActiveIoCircuitName = activeIoCircuitName;
			IoCircuitPort = ioCircuitPort;
			ActiveRangeMeterName = activeRangeMeterName;
			RangeMeterPort = rangeMeterPort;
			RangeMeterSubtractionValueMm = rangeMeterSubtractionValueMm;
			IpCameraSettings = ipCameraSettings;
			OutputPath = outputPath;
			ShutDownPcByDefault = shutDownPcByDefault;
		}

		public static IoSettings GetDefaultSettings()
		{
			var defaultScanners = new[] { new IoEntry("keyboard", "") };
			var defaultCameraSettings = IpCameraSettings.GetDefaultSettings();

			return new IoSettings("kinectv2", "massak", "", 5, defaultScanners, "keusb24r", "", "custom", "", 0,
				defaultCameraSettings, "MeasurementResults", false);
		}

		[OnDeserialized]
		private void OnDeserialized(StreamingContext context)
		{
			if (IpCameraSettings == null)
				IpCameraSettings = IpCameraSettings.GetDefaultSettings();

            if (ScalesMinWeight < 0)
                ScalesMinWeight = 1;
		}
	}
}