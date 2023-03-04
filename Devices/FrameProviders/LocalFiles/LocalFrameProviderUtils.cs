using System.Collections.Generic;
using System.IO;
using System.Linq;
using Primitives;
using ProcessingUtils;

namespace FrameProviders.LocalFiles
{
	internal static class LocalFrameProviderUtils
	{
		private const string FileFolder = "localFrames";
		private static readonly string ColorFramesPath = Path.Combine(FileFolder, "color");
		private static readonly string DepthFramesPath = Path.Combine(FileFolder, "depth");

		public static IReadOnlyList<ImageData> ReadImages()
		{
			if (!Directory.Exists(ColorFramesPath))
				return null;

			var colorFrameFiles = new DirectoryInfo(ColorFramesPath).EnumerateFiles().ToArray();
			
			return colorFrameFiles.Select(f => ImageUtils.ReadImageDataFromFile(f.FullName)).ToList();
		}

		public static IReadOnlyList<DepthMap> ReadDepthMaps()
		{
			if (!Directory.Exists(DepthFramesPath))
				return null;

			var depthFrameFiles = new DirectoryInfo(DepthFramesPath).EnumerateFiles().ToArray();
			
			return depthFrameFiles.Select(f => DepthMapUtils.ReadDepthMapFromRawFile(f.FullName))
				.ToList();
		}
	}
}