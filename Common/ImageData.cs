namespace Common
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
	}
}