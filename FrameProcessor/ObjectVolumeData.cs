namespace FrameProcessor
{
	public class ObjectVolumeData
	{
		public int LengthMm { get; }

		public int WidthMm { get; }

		public int HeightMm { get; }

		public ObjectVolumeData(int lengthMm, int widthMm, int heightMm)
		{
			LengthMm = lengthMm;
			WidthMm = widthMm;
			HeightMm = heightMm;
		}
	}
}