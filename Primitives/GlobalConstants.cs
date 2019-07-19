using System;
using System.Globalization;
using System.IO;

namespace Primitives
{
	public class GlobalConstants
	{
		public const string ManufacturerName = "IS";

		public const string AppTitle = "VolumeCalculator";

		public static readonly string CommonFolderPath = Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData);

		public static readonly string AppVersion = $"v1.49";

		public static readonly string AppHeaderString = $@"{ManufacturerName} {AppTitle} {AppVersion}";

		public static readonly string AppDataPath = Path.Combine(CommonFolderPath, AppTitle);

		public static readonly string AppLogsPath = Path.Combine(AppDataPath, "logs");

		public static readonly string AppConfigPath = Path.Combine(AppDataPath, "config");

		public static readonly string ConfigFileName = Path.Combine(AppConfigPath, "main.cfg");

		public static readonly string ResultFileName = "results.csv";

		public static readonly string CountersFileName = Path.Combine(AppConfigPath, "counters");

		public static string CsvSeparator = CultureInfo.CurrentCulture.TextInfo.ListSeparator;

		public static readonly string ResultsFileName = "results.csv";

		public static readonly string ResultPhotosFolder = "photos";
	}
}