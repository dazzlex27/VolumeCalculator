using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;

namespace Primitives
{
	public static class ImageUtils
	{
		public static ImageData ReadImageDataFromFile(string filepath)
		{
			var bitmap = new Bitmap(filepath);

			byte bytesPerPixel = 1;
			switch (bitmap.PixelFormat)
			{
				case PixelFormat.Format24bppRgb:
					bytesPerPixel = 3;
					break;
				case PixelFormat.Format32bppArgb:
					bytesPerPixel = 4;
					break;
			}

			var data = new byte[bitmap.Width * bitmap.Height * bytesPerPixel];
			var image = new ImageData(bitmap.Width, bitmap.Height, data, bytesPerPixel);

			var fullRect = new Rectangle(0, 0, bitmap.Width, bitmap.Height);
			var bmpData = bitmap.LockBits(fullRect, ImageLockMode.WriteOnly, bitmap.PixelFormat);

			Marshal.Copy(bmpData.Scan0, image.Data, 0, image.Data.Length);

			bitmap.UnlockBits(bmpData);

			return image;
		}

		public static void SaveImageDataToFile(ImageData image, string filepath)
		{
			var format = GetPixelFormatFromBpp(image.BytesPerPixel);

			var bitmap = new Bitmap(image.Width, image.Height, format);

			var fullRect = new Rectangle(0, 0, bitmap.Width, bitmap.Height);
			var bmpData = bitmap.LockBits(fullRect, ImageLockMode.WriteOnly, bitmap.PixelFormat);

			Marshal.Copy(image.Data, 0, bmpData.Scan0, image.Data.Length);

			bitmap.UnlockBits(bmpData);

			bitmap.Save(filepath);
		}

		private static PixelFormat GetPixelFormatFromBpp(int bytesPerPixel)
		{
			switch (bytesPerPixel)
			{
				case 1:
					return PixelFormat.Format8bppIndexed;
				case 3:
					return PixelFormat.Format24bppRgb;
				case 4:
					return PixelFormat.Format32bppArgb;
				default:
					return PixelFormat.Canonical;
			}
		}
	}
}