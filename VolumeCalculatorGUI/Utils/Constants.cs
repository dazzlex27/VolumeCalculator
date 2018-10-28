using System.Windows.Media;
using System.Globalization;
using System.IO;

namespace VolumeCalculatorGUI.Utils
{
	internal static class Constants
	{
		public const string AppTitle = "VolumeCalculator";

		public const string AppVersion = "v0.112 Beta";

		public static string AppHeaderString = $@"{AppTitle} {AppVersion}";

		public static readonly int BytesPerPixel24 = (PixelFormats.Bgr24.BitsPerPixel + 7) / 8;

		public static readonly int BytesPerPixel32 = (PixelFormats.Bgra32.BitsPerPixel + 7) / 8;

		public const string ConfigFileName = "settings.cfg";

		public const string ResultFileName = "results.csv";

		public const string PortsFileName = "ports";

		public static string CsvSeparator = CultureInfo.CurrentCulture.TextInfo.ListSeparator;

		public static string LocalFrameProviderFolderName = "localFrameProvider";

		public static string RealsenseFrameProviderFileName = "D435";

		public const string DebugDataDirectoryName = "out";
		public static readonly string DebugColorFrameFilename = Path.Combine(DebugDataDirectoryName, "color.png");
		public static readonly string DebugDepthFrameFilename = Path.Combine(DebugDataDirectoryName, "depth.png");
	}
}