using System.Globalization;

namespace Primitives
{
	internal static class Constants
	{
		public static readonly string ConfigFileName = "settings.cfg";

		public static readonly string ResultFileName = "results.csv";

		public static readonly string CountersFileName = "counters";

		public static string CsvSeparator = CultureInfo.CurrentCulture.TextInfo.ListSeparator;

		public static readonly string ResultsFileName = "results.csv";

		public static readonly string ResultPhotosFolder = "photos";
	}
}