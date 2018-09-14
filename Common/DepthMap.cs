using System;

namespace Common
{
	public class DepthMap
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

		public DepthMap(DepthMap depthMap)
		{
			Width = depthMap.Width;
			Height = depthMap.Height;
			Data = new short[Width * Height];

			if (depthMap.Data != null)
				Buffer.BlockCopy(depthMap.Data, 0, Data, 0, sizeof(short) * Data.Length);
		}
	}
}