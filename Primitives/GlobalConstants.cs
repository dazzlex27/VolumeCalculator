namespace Primitives
{
	public class GlobalConstants
	{
		public const string ManufacturerName = "IS";

		public const string AppTitle = "VolumeCalculator";

		public static readonly bool ProEdition = false;

		public static readonly string Edition = ProEdition ? "Pro" : "Standard";

		public static readonly string AppVersion = $"v1.09 {Edition}";

		public static readonly string AppHeaderString = $@"{ManufacturerName} {AppTitle} {AppVersion}";
	}
}