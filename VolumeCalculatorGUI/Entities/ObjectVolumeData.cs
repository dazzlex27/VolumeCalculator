namespace VolumeCalculatorGUI.Entities
{
	internal class ObjectVolumeData
	{
		public int Width { get; }

		public int Height { get; }

		public int Depth { get; }

		public long Volume { get; }

		public ObjectVolumeData(int width, int height, int depth)
		{
			Width = width;
			Height = height;
			Depth = depth;
			Volume = width * height * depth;
		}
	}
}