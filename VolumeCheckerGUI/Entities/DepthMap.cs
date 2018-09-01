namespace VolumeCheckerGUI.Entities
{
	internal class DepthMap
	{
		public int Width { get; }

		public int Height { get; }

		public short[] Data { get; }

		public DepthMap(int width, int height, short[] data)
		{
			Width = width;
			Height = height;
			Data = data;
		}
	}
}