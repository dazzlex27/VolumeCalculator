using System;
using System.IO;
using System.Threading.Tasks;
using Primitives;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Advanced;
using SixLabors.ImageSharp.Formats.Jpeg;
using SixLabors.ImageSharp.PixelFormats;

namespace ProcessingUtils
{
	public static class ImageUtils
	{
		public static async Task<ImageData> ReadImageDataFromFileAsync(string filepath)
		{
			var image = await Image.LoadAsync(filepath);

			return await GetImageDataFromImageAsync(image);
		}

		public static async Task SaveImageDataToFileAsync(ImageData imageData, string filepath)
		{
			var image = GetImageFromImageData(imageData);
			await image.SaveAsync(filepath);
		}

		public static async Task<string> GetBase64StringFromImageDataAsync(ImageData imageData)
		{
			if (imageData == null)
				return string.Empty;

			var image = GetImageFromImageData(imageData);
			var encoder = new JpegEncoder { Quality = 75 };
			var stream = new MemoryStream();
			await image.SaveAsync(stream, encoder);
			var bytes = stream.ToArray();

			var headerString = $"data:{JpegFormat.Instance.DefaultMimeType};base64";
			var contentString = Convert.ToBase64String(bytes);

			return $"{headerString},{contentString}";
		}

		public static async Task<ImageData> GetImageDataFromBase64StringAsync(string base64String)
		{
			if (string.IsNullOrWhiteSpace(base64String))
				return null;

			const string headerEnd = "base64,";
			var dataSubstringStartIndex = base64String.IndexOf("base64,");
			var dataSubstring = base64String.Substring(dataSubstringStartIndex + headerEnd.Length);
			var bytes = Convert.FromBase64String(dataSubstring);
			var stream = new MemoryStream(bytes);
			var image = await Image.LoadAsync(stream);

			return await GetImageDataFromImageAsync(image);
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

		public static async Task<ImageData> GetImageDataFromImageAsync(Image image)
		{
			int bytesPerPixel = image.PixelType.BitsPerPixel / 8;
			var imageData = new ImageData(image.Width, image.Height, (byte)bytesPerPixel);

			// TODO: this copies the entire image at once
			// TODO: copy one row at a time
			var visitor = new PixelCopyVisitor(imageData.Data);
			await image.AcceptVisitorAsync(visitor); // copy pixel data
			
			return imageData;
		}
	}
}
