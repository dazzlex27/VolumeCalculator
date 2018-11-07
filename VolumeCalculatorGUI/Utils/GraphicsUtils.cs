using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Primitives;

namespace VolumeCalculatorGUI.Utils
{
	internal static class GraphicsUtils
	{
		private static int DefaultDpi = 96;

		public static WriteableBitmap GetWriteableBitmapFromImageData(ImageData image)
		{
			var imageWidth = image.Width;
			var imageHeight = image.Height;
			var fullRect = new Int32Rect(0, 0, imageWidth, imageHeight);

			var format = GetFormatFromBytesPerPixel(image.BytesPerPixel);

			var colorImageBitmap = new WriteableBitmap(imageWidth, imageHeight, DefaultDpi, DefaultDpi, format, null);
			colorImageBitmap.WritePixels(fullRect, image.Data, image.Stride, 0);
			colorImageBitmap.Freeze();

			return colorImageBitmap;
		}

		public static PixelFormat GetFormatFromBytesPerPixel(int bytesPerPixel)
		{
			switch (bytesPerPixel)
			{
				case 3:
					return PixelFormats.Rgb24;
				case 4:
					return PixelFormats.Bgra32;
				default:
					return PixelFormats.BlackWhite;
			}
		}
	}
}