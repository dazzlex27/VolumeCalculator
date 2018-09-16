using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Runtime.InteropServices;

namespace Common
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

			using (var fs = File.AppendText(filepath))
			{
				fs.WriteLine(depthMap.Width);
				fs.WriteLine(depthMap.Height);

				foreach(var pixel in depthMap.Data)
					fs.WriteLine(pixel);
			}
		}

		public static void SaveDepthMapImageToFile(DepthMap map, string filepath, short minDepth, short maxDepth, short cutOffDepth)
		{
			var bitmap = new Bitmap(map.Width, map.Height, PixelFormat.Format24bppRgb);

			var fullRect = new Rectangle(0, 0, bitmap.Width, bitmap.Height);
			var bmpData = bitmap.LockBits(fullRect, ImageLockMode.WriteOnly, bitmap.PixelFormat);

			var data = GetColorizedDepthMapData(map, minDepth, maxDepth, cutOffDepth);
			Marshal.Copy(data, 0, bmpData.Scan0, data.Length);

			bitmap.UnlockBits(bmpData);

			bitmap.Save(filepath);
		}

		public static byte[] GetColorizedDepthMapData(DepthMap map, short minDepth, short maxDepth, short cutOffDepth)
		{
			var resultBytes = new byte[map.Data.Length * 3];

			var byteIndex = 0;
			foreach (var depthValue in map.Data)
			{
				var intensity = depthValue > cutOffDepth
					? (byte)0
					: GetIntensityFromDepth(depthValue, minDepth, maxDepth);

				resultBytes[byteIndex++] = intensity;
				resultBytes[byteIndex++] = intensity;
				resultBytes[byteIndex++] = intensity;
			}

			return resultBytes;
		}

		private static byte GetIntensityFromDepth(short depth, short minValue, short maxValue)
		{
			if (depth < minValue)
				return 0;

			return (byte) (255 - 255 * (depth - minValue) / maxValue);
		}
	}
}