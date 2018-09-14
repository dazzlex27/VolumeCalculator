namespace VolumeCalculatorGUI.Entities
{
	internal class WorkingAreaMask
	{
		public int Width { get; }

		public int Height { get; }

		public bool[] Data { get; }

		public WorkingAreaMask(int width, int height, bool[] data)
		{
			Width = width;
			Height = height;
			Data = data;
		}
	}
}