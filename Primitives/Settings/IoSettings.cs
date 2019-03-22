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

		public int RangeMeterSubtractionValueMm { get; set; }

		public string OutputPath { get; set; }

		public bool ShutDownPcByDefault { get; set; }

		public string ResultsFilePath => Path.Combine(OutputPath, GlobalConstants.ResultsFileName);

		public string PhotosDirectoryPath => Path.Combine(OutputPath, GlobalConstants.ResultPhotosFolder);

		public IoSettings(string activeCameraName, string activeScalesName, string scalesPort, IoEntry[] activeScanners,
			string activeIoCircuitName, string ioCircuitPort, string activeRangeMeterName, string rangeMeterPort,
			int rangeMeterSubtractionValueMm, string outputPath, bool shutDownPcByDefault)
		{
			ActiveCameraName = activeCameraName;
			ActiveScalesName = activeScalesName;
			ScalesPort = scalesPort;
			ActiveScanners = activeScanners;
			ActiveIoCircuitName = activeIoCircuitName;
			IoCircuitPort = ioCircuitPort;
			ActiveRangeMeterName = activeRangeMeterName;
			RangeMeterPort = rangeMeterPort;
			RangeMeterSubtractionValueMm = rangeMeterSubtractionValueMm;
			OutputPath = outputPath;
			ShutDownPcByDefault = shutDownPcByDefault;
		}

		public static IoSettings GetDefaultSettings()
		{
			var defaultScanners = new[] {new IoEntry("keyboard", "")};

			if (GlobalConstants.ProEdition)
			{
				return new IoSettings("kinectv2", "casm", "COM1", defaultScanners, "keusb24r", "", "custom", "", 0,
					"MeasurementResults", false);
			}
			else
			return new IoSettings("kinectv2", "massak", "COM1", defaultScanners, "keusb24r", "", "custom", "", 0,
				"MeasurementResults", false);
		}
	}
}