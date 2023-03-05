using System;

namespace Primitives
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

		public DepthMap(int width, int height)
			: this(width, height, new short[width * height])
		{
		}

		public DepthMap(DepthMap depthMap)
		{
			Width = depthMap.Width;
			Height = depthMap.Height;
			Data = depthMap.Data == null ? null : new short[Width * Height];

			if (depthMap.Data != null)
				Buffer.BlockCopy(depthMap.Data, 0, Data, 0, sizeof(short) * Data.Length);
		}
	}
}
