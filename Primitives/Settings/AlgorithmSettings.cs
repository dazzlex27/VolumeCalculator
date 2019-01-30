using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Windows;

namespace Primitives.Settings
{
	public class AlgorithmSettings
	{
		public short FloorDepth { get; set; }

		public short MinObjectHeight { get; set; }

		public byte SampleDepthMapCount { get; set; }

		public bool UseColorMask { get; set; }

		public List<Point> ColorMaskContour { get; set; }

		public bool UseDepthMask { get; set; }

		public List<Point> DepthMaskContour { get; set; }

		public long TimeToStartMeasurementMs { get; set; }

		public bool EnableDmAlgorithm { get; set; }

		public bool EnablePerspectiveDmAlgorithm { get; set; }

		public bool EnableRgbAlgorithm { get; set; }

		public AlgorithmSettings(short floorDepth, short minObjectHeight, byte sampleDepthMapCount, bool useColorMask, 
			IEnumerable<Point> colorMaskContour, bool useDepthMask, IEnumerable<Point> depthMaskContour, long timeToStartMeasurementMs)
		{
			FloorDepth = floorDepth;
			MinObjectHeight = minObjectHeight;
			SampleDepthMapCount = sampleDepthMapCount;
			UseColorMask = useColorMask;
			ColorMaskContour = new List<Point>(colorMaskContour);
			UseDepthMask = useDepthMask;
			DepthMaskContour = new List<Point>(depthMaskContour);
			TimeToStartMeasurementMs = timeToStartMeasurementMs;
			EnableDmAlgorithm = true;
			EnablePerspectiveDmAlgorithm = true;
			EnableRgbAlgorithm = false;
		}

		public static AlgorithmSettings GetDefaultSettings()
		{
			return new AlgorithmSettings(1000, 5, 10, false, GetDefaultAreaContour(), false, GetDefaultAreaContour(), 5000);
		}

		public override string ToString()
		{
			return $"floorDepth={FloorDepth} useColorMask={UseColorMask} useDepthMask={UseDepthMask} minObjHeight={MinObjectHeight} sampleCount={SampleDepthMapCount}";
		}

		[OnDeserializing]
		private void OnDeserialize(StreamingContext context)
		{
			if (FloorDepth <= 0)
				FloorDepth = 1000;

			if (SampleDepthMapCount <= 0)
				SampleDepthMapCount = 10;

			if (ColorMaskContour == null)
				ColorMaskContour = GetDefaultAreaContour();

			if (DepthMaskContour == null)
				DepthMaskContour = GetDefaultAreaContour();

			if (TimeToStartMeasurementMs <= 0)
				TimeToStartMeasurementMs = 5000;

			EnableRgbAlgorithm = false;
		}

		private static List<Point> GetDefaultAreaContour()
		{
			return new List<Point>
			{
				new Point(0.2, 0.2),
				new Point(0.2, 0.8),
				new Point(0.8, 0.8),
				new Point(0.8, 0.2)
			};
		}
	}
}