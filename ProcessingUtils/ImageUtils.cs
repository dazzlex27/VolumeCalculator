using System;
using System.IO;
using Primitives;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Advanced;
using SixLabors.ImageSharp.Formats.Jpeg;
using SixLabors.ImageSharp.PixelFormats;

namespace ProcessingUtils
{
	public static class ImageUtils
	{
		public static ImageData ReadImageDataFromFile(string filepath)
		{
			var image = Image.Load(filepath);

			return GetImageDataFromImage(image);
		}

		public static void SaveImageDataToFile(ImageData imageData, string filepath)
		{
			var image = GetImageFromImageData(imageData);
			image.Save(filepath);
		}

		public static string GetBase64StringFromImageData(ImageData imageData)
		{
			if (imageData == null)
				return string.Empty;

			var image = GetImageFromImageData(imageData);
			var encoder = new JpegEncoder { Quality = 80 };
			var stream = new MemoryStream();
			image.Save(stream, encoder);
			var bytes = stream.ToArray();

			return Convert.ToBase64String(bytes);
		}

		public static ImageData GetImageDataFromBase64String(string base64String)
		{
			if (string.IsNullOrWhiteSpace(base64String))
				return null;

			var bytes = Convert.FromBase64String(base64String);
			var stream = new MemoryStream(bytes);
			var image = Image.Load(stream);

			return GetImageDataFromImage(image);
		}

		public static Image GetImageFromImageData(ImageData imageData)
		{
			var bpp = imageData.BytesPerPixel;
			var data = imageData.Data;
			var w = imageData.Width;
			var h = imageData.Height;

			switch (bpp)
			{
				case 1:
					return Image.LoadPixelData<L8>(data, w, h);
				case 3:
					return Image.LoadPixelData<Bgr24>(data, w, h);
				case 4:
					return Image.LoadPixelData<Bgra32>(data, w, h);
				default:
					throw new NotImplementedException(
						$"Bytes per pixel value {bpp} is not supported as a pixel format");
			}
		}

		public static ImageData GetImageDataFromImage(Image image)
		{
			int bytesPerPixel = image.PixelType.BitsPerPixel / 8;
			var imageData = new ImageData(image.Width, image.Height, (byte)bytesPerPixel);

			// TODO: this copies the entire image at once
			// TODO: copy one row at a time
			var visitor = new PixelCopyVisitor(imageData.Data);
			image.AcceptVisitor(visitor); // copy pixel data
			
			return imageData;
		}
	}
}