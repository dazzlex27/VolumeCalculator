using System.Windows.Media;
using System.Globalization;

namespace VolumeCalculatorGUI.Utils
{
	internal static class Constants
	{
		public const string AppTitle = "VolumeCalculator";

		public const string AppVersion = "v0.107 Beta";

		public static string AppHeaderString = $@"{AppTitle} {AppVersion}";

		public static readonly int BytesPerPixel24 = (PixelFormats.Bgr24.BitsPerPixel + 7) / 8;

		public static readonly int BytesPerPixel32 = (PixelFormats.Bgra32.BitsPerPixel + 7) / 8;

		public static readonly string ResultFileName = "results.csv";

		public static string CsvSeparator = CultureInfo.CurrentCulture.TextInfo.ListSeparator;

		public static string LocalFrameProviderFolderName = "localFrameProvider";
	}
}