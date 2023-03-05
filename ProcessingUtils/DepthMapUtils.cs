using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace Primitives
{
	public static class DepthMapUtils
	{
		public static async Task<DepthMap> ReadDepthMapFromRawFileAsync(string filepath)
		{
			var fileLines = await File.ReadAllLinesAsync(filepath);

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

		public static async Task SaveDepthMapToRawFileAsync(DepthMap depthMap, string filepath)
		{
			var builder = new StringBuilder();
			builder.AppendLine(depthMap.Width.ToString());
			builder.AppendLine(depthMap.Height.ToString());

			foreach (var pixel in depthMap.Data)
				builder.AppendLine(pixel.ToString());

			await File.WriteAllTextAsync(filepath, builder.ToString());
		}

		public static async Task SaveDepthMapImageToFile(DepthMap map, string filepath,
			short minDepth, short maxDepth, short cutOffDepth = short.MaxValue)
		{
			var filteredMap = GetDepthFilteredDepthMap(map, cutOffDepth);
			var filteredMapImageData = GetColorizedDepthMapData(filteredMap, minDepth, maxDepth);
			var image = Image.LoadPixelData<L8>(filteredMapImageData.Data, map.Width, map.Height);
			await image.SaveAsync(filepath);
		}

		public static ImageData GetColorizedDepthMapData(DepthMap map, short minDepth, short maxDepth)
		{
			var resultBytes = new byte[map.Data.Length];

			var byteIndex = 0;
			foreach (var depthValue in map.Data)
				resultBytes[byteIndex++] = GetIntensityFromDepth(depthValue, minDepth, maxDepth);

			return new ImageData(map.Width, map.Height, resultBytes, 1);
		}

		public static DepthMap GetDepthFilteredDepthMap(DepthMap depthMap, short thresholdDepth)
		{
			var filteredMap = new DepthMap(depthMap);

			for (var i = 0; i < filteredMap.Data.Length; i++)
			{
				if (filteredMap.Data[i] > thresholdDepth)
					filteredMap.Data[i] = 0;
			}

			return filteredMap;
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

		public static ImageData GetGrayscaleImageDataFromfDepthMap(DepthMap map, short minDepth, short maxDepth)
		{
			var resultBytes = new byte[map.Data.Length];

			var byteIndex = 0;
			foreach (var depthValue in map.Data)
				resultBytes[byteIndex++] = GetIntensityFromDepth(depthValue, minDepth, maxDepth);

			return new ImageData(map.Width, map.Height, resultBytes, 1);
		}
	}
}
