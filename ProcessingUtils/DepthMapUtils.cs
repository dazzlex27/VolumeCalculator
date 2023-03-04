using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using System.ComponentModel.DataAnnotations;
using System.IO;

namespace Primitives
{
	public static class DepthMapUtils
	{
		public static DepthMap ReadDepthMapFromRawFile(string filepath)
		{
			var fileLines = File.ReadAllLines(filepath);

			if (fileLines.Length < 3)
				throw new InvalidDataException("Invalid depth map format");

			var width = int.Parse(fileLines[0]);
			var height = int.Parse(fileLines[1]);
			var length = width * height;
			var data = new short[length];

			for (var i = 0; i < length; i++)
				data[i] = short.Parse(fileLines[i + 2]);

			return new DepthMap(width, height, data);
		}

		public static void SaveDepthMapToRawFile(DepthMap depthMap, string filepath)
		{
			if (File.Exists(filepath))
				File.Delete(filepath);

			using var fs = File.AppendText(filepath);
			fs.WriteLine(depthMap.Width);
			fs.WriteLine(depthMap.Height);

			foreach (var pixel in depthMap.Data)
				fs.WriteLine(pixel);
		}

		public static void SaveDepthMapImageToFile(DepthMap map, string filepath,
			short minDepth, short maxDepth, short cutOffDepth)
		{
			var copyMap = new DepthMap(map);
			FilterDepthMapByDepthtLimit(copyMap, cutOffDepth);

			var filteredData = GetColorizedDepthMapData(copyMap, minDepth, maxDepth);
			var image = Image.LoadPixelData<L8>(filteredData, map.Width, map.Height);
			image.Save(filepath);
		}

		public static byte[] GetColorizedDepthMapData(DepthMap map, short minDepth, short maxDepth)
		{
			var resultBytes = new byte[map.Data.Length];

			var byteIndex = 0;
			foreach (var depthValue in map.Data)
				resultBytes[byteIndex++] = GetIntensityFromDepth(depthValue, minDepth, maxDepth);

			return resultBytes;
		}

		public static void FilterDepthMapByDepthtLimit(DepthMap depthMap, short maxDepth)
		{
			for (var i = 0; i < depthMap.Data.Length; i++)
			{
				if (depthMap.Data[i] > maxDepth)
					depthMap.Data[i] = 0;
			}
		}

		public static byte GetIntensityFromDepth(short depth, short minValue, short maxValue)
		{
			if (depth < minValue)
				return 0;

			if (depth > maxValue)
				return 0;

			if (maxValue == 0)
				return 0;

			return (byte) (255 - 255 * (depth - minValue) / maxValue);
		}

		public static byte[] GetGrayscaleDepthMapDataBgr(DepthMap map, short minDepth, short maxDepth)
		{
			var resultBytes = new byte[map.Data.Length];

			var byteIndex = 0;
			foreach (var depthValue in map.Data)
				resultBytes[byteIndex++] = GetIntensityFromDepth(depthValue, minDepth, maxDepth);

			return resultBytes;
		}
	}
}