using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Common;

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

		//public static WriteableBitmap GetWriteableBitmapFromDepthMap(DepthMap depthMap)
		//{

		//}

		public static PixelFormat GetFormatFromBytesPerPixel(int bytesPerPixel)
		{
			var format = PixelFormats.BlackWhite;
			switch (bytesPerPixel)
			{
				case 3:
					format = PixelFormats.Rgb24;
					bytesPerPixel = Constants.BytesPerPixel24;
					break;
				case 4:
					format = PixelFormats.Bgra32;
					bytesPerPixel = Constants.BytesPerPixel32;
					break;
			}

			return format;
		}
	}
}
