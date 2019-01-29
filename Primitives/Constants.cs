using System.Globalization;

namespace Primitives
{
	internal static class Constants
	{
		public const string ConfigFileName = "settings.cfg";

		public const string ResultFileName = "results.csv";

		public const string CountersFileName = "counters";

		public static string CsvSeparator = CultureInfo.CurrentCulture.TextInfo.ListSeparator;

		public const string ResultsFileName = "results.csv";
		public const string ResultPhotosFolder = "photos";
	}
}