using System.IO;

namespace FrameProcessor
{
	internal static class Constants
	{
		public const string AnalyzerLibName = "libDepthMapProcessor.dll";

		public const string DebugDataDirectoryName = "out";
		public static readonly string DebugColorFrameFilename = Path.Combine(DebugDataDirectoryName, "color.png");
		public static readonly string DebugDepthFrameFilename = Path.Combine(DebugDataDirectoryName, "depth.png");
	}
}