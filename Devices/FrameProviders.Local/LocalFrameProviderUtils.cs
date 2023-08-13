using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Primitives;
using ProcessingUtils;

namespace FrameProviders.Local
{
	internal static class LocalFrameProviderUtils
	{
		private const string FileFolder = "localFrames";
		private static readonly string ColorFramesPath = Path.Combine(FileFolder, "color");
		private static readonly string DepthFramesPath = Path.Combine(FileFolder, "depth");

		public static async Task<IReadOnlyList<ImageData>> ReadImagesAsync()
		{
			if (!Directory.Exists(ColorFramesPath))
				return null;

			var colorFrameFiles = new DirectoryInfo(ColorFramesPath).EnumerateFiles();

			var readImages = new List<ImageData>();
			foreach (var file in colorFrameFiles)
			{
				var image = await ImageUtils.ReadImageDataFromFileAsync(file.FullName);
				readImages.Add(image);
			}

			return readImages;
		}

		public static async Task<IReadOnlyList<DepthMap>> ReadDepthMapsAsync()
		{
			if (!Directory.Exists(DepthFramesPath))
				return null;

			var depthFrameFiles = new DirectoryInfo(DepthFramesPath).EnumerateFiles();

			var readMaps = new List<DepthMap>();
			foreach (var file in depthFrameFiles)
			{
				var map = await DepthMapUtils.ReadDepthMapFromRawFileAsync(file.FullName);
				readMaps.Add(map);
			}

			return readMaps;
		}
	}
}
