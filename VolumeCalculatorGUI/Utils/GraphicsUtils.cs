using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Primitives;

namespace VolumeCalculatorGUI.Utils
{
	internal static class GraphicsUtils
	{
		public static WriteableBitmap GetWriteableBitmapFromImageData(ImageData image)
		{
			var format = GetFormatFromBytesPerPixel(image.BytesPerPixel);
			var colorImageBitmap = new WriteableBitmap(image.Width, image.Height, Constants.DefaultDpi,
				Constants.DefaultDpi, format, null);

			var fullRect = new Int32Rect(0, 0, image.Width, image.Height);
			colorImageBitmap.WritePixels(fullRect, image.Data, image.Stride, 0);
			colorImageBitmap.Freeze();

			return colorImageBitmap;
		}

		public static PixelFormat GetFormatFromBytesPerPixel(int bytesPerPixel)
		{
			switch (bytesPerPixel)
			{
				case 1:
					return PixelFormats.Gray8;
				case 2:
					return PixelFormats.Gray16;
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