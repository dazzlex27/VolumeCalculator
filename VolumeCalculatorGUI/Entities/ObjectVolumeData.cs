namespace VolumeCalculatorGUI.Entities
{
	internal class ObjectVolumeData
	{
		public int Length { get; }

		public int Width { get; }

		public int Height { get; }

		public long Volume { get; }

		public ObjectVolumeData(int length, int width, int height)
		{
			Length = length;
			Width = width;
			Height = height;
			Volume = width * height * length;
		}
	}
}