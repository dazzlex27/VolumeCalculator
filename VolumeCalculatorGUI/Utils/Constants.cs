using System.Globalization;

namespace VolumeCalculatorGUI.Utils
{
	internal static class Constants
	{
		public const string ManufacturerName = "IS";

		public const string AppTitle = "VolumeCalculator";

		public const string AppVersion = "v0.125 Beta";

		public static string AppHeaderString = $@"{ManufacturerName} {AppTitle} {AppVersion}";

		public static string CsvSeparator = CultureInfo.CurrentCulture.TextInfo.ListSeparator;

		public static string LocalFrameProviderFolderName = "localFrameProvider";

		public static string RealsenseFrameProviderFileName = "D435";

		public const int ScalesPollingRateMs = 1000;
	}
}