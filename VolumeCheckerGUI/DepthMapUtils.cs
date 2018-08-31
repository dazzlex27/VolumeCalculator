using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using VolumeCheckerGUI.Structures;

namespace VolumeCheckerGUI
{
	internal static class DepthMapUtils
	{
		public static Bitmap GetBitmapFromDepthMap(DepthMap map, short minDepth, short maxDepth, short cutOffDepth)
		{
			var bmp = new Bitmap(map.Width, map.Height, PixelFormat.Format24bppRgb);

			var imageData = GetImageBytesFromDepthPixels(map.Data, minDepth, maxDepth, cutOffDepth);

			var fullRect = new Rectangle(0, 0, bmp.Width, bmp.Height);
			var bmpData = bmp.LockBits(fullRect, ImageLockMode.WriteOnly, bmp.PixelFormat);

			Marshal.Copy(imageData, 0, bmpData.Scan0, imageData.Length);

			bmp.UnlockBits(bmpData);

			return bmp;
		}

		private static byte[] GetImageBytesFromDepthPixels(IReadOnlyCollection<short> depthPixels, short minDepth, 
			short maxDepth, short cutOffDepth)
		{
			var resultBytes = new byte[depthPixels.Count * 3];

			var byteIndex = 0;
			foreach (var depthValue in depthPixels)
			{
				var intensity = depthValue > cutOffDepth
					? (byte) 0
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