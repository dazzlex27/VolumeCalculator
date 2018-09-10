using System.Collections.Generic;
using DepthMapProcessorGUI.Entities;
using VolumeCalculatorGUI.Entities;

namespace DepthMapProcessorGUI.Utils
{
	internal static class DepthMapUtils
	{
		public static byte[] GetColorizedDepthMapData(DepthMap map, short minDepth, short maxDepth, short cutOffDepth)
		{
			var resultBytes = new byte[map.Data.Length * 3];

			var byteIndex = 0;
			foreach (var depthValue in map.Data)
			{
				var intensity = depthValue > cutOffDepth
					? (byte)0
					: GetIntensityFromDepth(depthValue, minDepth, maxDepth);

				resultBytes[byteIndex++] = intensity;
				resultBytes[byteIndex++] = intensity;
				resultBytes[byteIndex++] = intensity;
			}

			return resultBytes;
		}

		private static byte GetIntensityFromDepth(short depth, short minValue, short maxValue)
		{
			if (depth < minValue)
				return 0;

			return (byte) (255 - 255 * (depth - minValue) / maxValue);
		}
	}
}