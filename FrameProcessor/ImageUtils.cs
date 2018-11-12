using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using Primitives;

namespace FrameProcessor
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
			var format = PixelFormat.Canonical;
			switch (image.BytesPerPixel)
			{
				case 3:
					format = PixelFormat.Format24bppRgb;
					break;
				case 4:
					format = PixelFormat.Format32bppArgb;
					break;
			}

			var bitmap = new Bitmap(image.Width, image.Height, format);

			var fullRect = new Rectangle(0, 0, bitmap.Width, bitmap.Height);
			var bmpData = bitmap.LockBits(fullRect, ImageLockMode.WriteOnly, bitmap.PixelFormat);

			Marshal.Copy(image.Data, 0, bmpData.Scan0, image.Data.Length);

			bitmap.UnlockBits(bmpData);

			bitmap.Save(filepath);
		}
	}
}