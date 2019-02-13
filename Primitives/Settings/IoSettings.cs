using System.IO;

namespace Primitives.Settings
{
	public class IoSettings
	{
		public string ActiveCameraName { get; set; }

		public string ActiveScalesName { get; set; }

		public string ScalesPort { get; set; }

		public IoEntry[] ActiveScanners { get; set; }

		public string ActiveIoCircuitName { get; set; }

		public string IoCircuitPort { get; set; }

		public string ActiveRangeMeterName { get; set; }

		public string RangeMeterPort { get; set; }

		public string OutputPath { get; set; }

		public bool ShutDownPcByDefault { get; set; }

		public string ResultsFilePath => Path.Combine(OutputPath, Constants.ResultsFileName);

		public string PhotosDirectoryPath => Path.Combine(OutputPath, Constants.ResultPhotosFolder);

		public IoSettings(string activeCameraName, string activeScalesName, string scalesPort, IoEntry[] activeScanners,
			string activeIoCircuitName, string ioCircuitPort, string activeRangeMeterName, string rangeMeterPort,
			string outputPath, bool shutDownPcByDefault)
		{
			ActiveCameraName = activeCameraName;
			ActiveScalesName = activeScalesName;
			ScalesPort = scalesPort;
			ActiveScanners = activeScanners;
			ActiveIoCircuitName = activeIoCircuitName;
			IoCircuitPort = ioCircuitPort;
			ActiveRangeMeterName = activeRangeMeterName;
			RangeMeterPort = rangeMeterPort;
			OutputPath = outputPath;
			ShutDownPcByDefault = shutDownPcByDefault;
		}

		public static IoSettings GetDefaultSettings()
		{
			var defaultScanners = new[] {new IoEntry("keyboard", "")};

			return new IoSettings("kinectv2", "massak", "", defaultScanners, "keusb24r", "", "custom", "",
				"MeasurementResults", false);
		}
	}
}