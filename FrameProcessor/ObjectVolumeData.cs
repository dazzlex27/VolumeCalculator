namespace FrameProcessor
{
	public class ObjectVolumeData
	{
		public int LengthMm { get; }

		public int WidthMm { get; }

		public int HeightMm { get; }

		public double VolumeCmCb { get; }

		public ObjectVolumeData(int lengthMm, int widthMm, int heightMm, double volumeCmCb)
		{
			LengthMm = lengthMm;
			WidthMm = widthMm;
			HeightMm = heightMm;
			VolumeCmCb = volumeCmCb;
		}
	}
}