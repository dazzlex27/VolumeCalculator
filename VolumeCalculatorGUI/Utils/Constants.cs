namespace VolumeCalculatorGUI.Utils
{
	internal static class Constants
	{
		public const string ManufacturerName = "IS";

		public const string AppTitle = "VolumeCalculator";

		public const string AppVersion = "v0.132 Beta";

		public static string AppHeaderString = $@"{ManufacturerName} {AppTitle} {AppVersion}";

		public static string LocalFrameProviderFolderName = "localFrameProvider";

		public static string RealsenseFrameProviderFileName = "D435";

		public static string FakeScalesFileName = "fakescales";

		public const int ScalesPollingRateMs = 1000;

		public const int DefaultDpi = 96;
	}
}