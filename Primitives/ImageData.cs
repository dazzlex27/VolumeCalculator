using System;

namespace Primitives
{
	public class ImageData
	{
		public int Width { get; }

		public int Height { get; }

		public byte[] Data { get; }

		public byte BytesPerPixel { get; }

		public int Stride => Width * BytesPerPixel;

		public ImageData(int width, int height, byte[] data, byte bytesPerPixel)
		{
			Width = width;
			Height = height;
			Data = data;
			BytesPerPixel = bytesPerPixel;
		}

		public ImageData(int width, int height, byte bytesPerPixel)
			: this(width, height, new byte[width * height * bytesPerPixel], bytesPerPixel)
		{
		}

		public ImageData(ImageData imageData)
		{
			Width = imageData.Width;
			Height = imageData.Height;
			BytesPerPixel = imageData.BytesPerPixel;
			Data = imageData.Data == null ? null : new byte[Width * Height * BytesPerPixel];

			if (imageData.Data != null)
				Buffer.BlockCopy(imageData.Data, 0, Data, 0, sizeof(byte) * Data.Length);
		}
	}
}
