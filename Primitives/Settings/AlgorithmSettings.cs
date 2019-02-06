using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Windows;

namespace Primitives.Settings
{
	public class AlgorithmSettings
	{
		private const int DefaultFloorDepthMm = 1000;
		private const int DefaultTimerValueMs = 3000;
		private const int DefaultSampleCount = 10;
		private const int DefaultMinObjHeightMm = 10;

		public short FloorDepth { get; set; }

		public short MinObjectHeight { get; set; }

		public byte SampleDepthMapCount { get; set; }

		public bool UseColorMask { get; set; }

		public List<Point> ColorMaskContour { get; set; }

		public bool UseDepthMask { get; set; }

		public List<Point> DepthMaskContour { get; set; }

		public bool EnableAutoTimer { get; set; }

		public long TimeToStartMeasurementMs { get; set; }

		public bool EnableDmAlgorithm { get; set; }

		public bool EnablePerspectiveDmAlgorithm { get; set; }

		public bool EnableRgbAlgorithm { get; set; }

		public bool RequireBarcode { get; set; }

		public AlgorithmSettings(short floorDepth, short minObjectHeight, byte sampleDepthMapCount, bool useColorMask,
			IEnumerable<Point> colorMaskContour, bool useDepthMask, IEnumerable<Point> depthMaskContour,
			bool enableAutoTimer, long timeToStartMeasurementMs, bool requireBarcode)
		{
			FloorDepth = floorDepth;
			MinObjectHeight = minObjectHeight;
			SampleDepthMapCount = sampleDepthMapCount;
			UseColorMask = useColorMask;
			ColorMaskContour = new List<Point>(colorMaskContour);
			UseDepthMask = useDepthMask;
			DepthMaskContour = new List<Point>(depthMaskContour);
			EnableAutoTimer = enableAutoTimer;
			TimeToStartMeasurementMs = timeToStartMeasurementMs;
			EnableDmAlgorithm = true;
			EnablePerspectiveDmAlgorithm = true;
			EnableRgbAlgorithm = GlobalConstants.ProEdition;
			RequireBarcode = requireBarcode;
		}

		public static AlgorithmSettings GetDefaultSettings()
		{
			return new AlgorithmSettings(DefaultFloorDepthMm, DefaultMinObjHeightMm, DefaultSampleCount, false,
				GetDefaultAreaContour(), false, GetDefaultAreaContour(), true, DefaultTimerValueMs, true);
		}

		public override string ToString()
		{
			return $"floorDepth={FloorDepth} useColorMask={UseColorMask} useDepthMask={UseDepthMask} minObjHeight={MinObjectHeight} sampleCount={SampleDepthMapCount}";
		}

		[OnDeserialized]
		private void OnDeserialized(StreamingContext context)
		{
			if (FloorDepth <= 0)
				FloorDepth = DefaultFloorDepthMm;

			if (MinObjectHeight <= 0)
				MinObjectHeight = DefaultMinObjHeightMm;

			if (SampleDepthMapCount <= 0)
				SampleDepthMapCount = DefaultSampleCount;

			if (ColorMaskContour == null)
				ColorMaskContour = GetDefaultAreaContour();

			if (DepthMaskContour == null)
				DepthMaskContour = GetDefaultAreaContour();

			if (TimeToStartMeasurementMs <= 0)
				TimeToStartMeasurementMs = DefaultTimerValueMs;

			if (!GlobalConstants.ProEdition)
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