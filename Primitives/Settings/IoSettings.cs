using System.IO;
using System.Reflection;

namespace Primitives.Settings
{
	public class IoSettings
	{
		[Obfuscation]
		public string ActiveCameraName { get; set; }

		[Obfuscation]
		public string ActiveScalesName { get; set; }

		[Obfuscation]
		public string ScalesPort { get; set; }

		[Obfuscation]
		public IoEntry[] ActiveScanners { get; set; }

		[Obfuscation]
		public string ActiveIoCircuitName { get; set; }

		[Obfuscation]
		public string IoCircuitPort { get; set; }

		[Obfuscation]
		public string OutputPath { get; set; }

		[Obfuscation]
		public bool ShutDownPcByDefault { get; set; }

		[Obfuscation]
		public string ResultsFilePath => Path.Combine(OutputPath, Constants.ResultsFileName);

		[Obfuscation]
		public string PhotosDirectoryPath => Path.Combine(OutputPath, Constants.ResultPhotosFolder);

		public IoSettings(string activeCameraName, string activeScalesName, string scalesPort, IoEntry[] activeScanners,
			string activeIoCircuitName, string ioCircuitPort, string outputPath, bool shutDownPcByDefault)
		{
			ActiveCameraName = activeCameraName;
			ActiveScalesName = activeScalesName;
			ScalesPort = scalesPort;
			ActiveScanners = activeScanners;
			ActiveIoCircuitName = activeIoCircuitName;
			IoCircuitPort = ioCircuitPort;
			OutputPath = outputPath;
			ShutDownPcByDefault = shutDownPcByDefault;
		}

		public static IoSettings GetDefaultSettings()
		{
			var defaultScanners = new[] {new IoEntry("keyboard", "")};

			return new IoSettings("kinectv2", "massak", "", defaultScanners, "keusb24r", "", "MeasurementResults",
				false);
		}
	}
}