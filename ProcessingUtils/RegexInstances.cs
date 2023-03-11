using System.Text.RegularExpressions;

namespace ProcessingUtils
{
	public static partial class RegexInstances
	{
		public static Regex NaturalNumberValidator = NaturalNumberRegex();

		[GeneratedRegex("[0-9]+")]
		public static partial Regex NaturalNumberRegex();

		public static Regex IntegerValidator = IntegerValidationRegex();

		[GeneratedRegex("(-)?([0-9]+)?")]
		private static partial Regex IntegerValidationRegex();
	}
}
