using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Text;
using System.Windows;

namespace Primitives.Settings
{
	public class AlgorithmSettings
	{
		private const int DefaultFloorDepthMm = 1805;
		private const int DefaultTimerValueMs = 1000;
		private const int DefaultSampleCount = 5;
		private const int DefaultMinObjHeightMm = 15;

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

		public WeightUnits SelectedWeightUnits { get; set; }

		public AlgorithmSettings(short floorDepth, short minObjectHeight, byte sampleDepthMapCount, bool useColorMask,
			IEnumerable<Point> colorMaskContour, bool useDepthMask, IEnumerable<Point> depthMaskContour,
			bool enableAutoTimer, long timeToStartMeasurementMs, bool requireBarcode, WeightUnits selectedWeightUnits)
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
			EnableRgbAlgorithm = true;
			RequireBarcode = requireBarcode;
			SelectedWeightUnits = selectedWeightUnits;
		}

		public static AlgorithmSettings GetDefaultSettings()
		{
			return new AlgorithmSettings(DefaultFloorDepthMm, DefaultMinObjHeightMm, DefaultSampleCount, false,
				GetDefaultAreaContour(), true, GetDefaultAreaContour(), true, DefaultTimerValueMs, true, WeightUnits.Gr);
		}

		public override string ToString()
		{
			var builder = new StringBuilder("Alg setings:");
			builder.AppendLine($"floorDepth={FloorDepth}");
			builder.AppendLine($"useColorMask={UseColorMask}");
			builder.AppendLine($"useDepthMask={UseDepthMask}");
			builder.AppendLine($"minObjHeight={MinObjectHeight}");
			builder.AppendLine($"sampleCount={SampleDepthMapCount}");
			builder.AppendLine($"enableAutTimer={EnableAutoTimer}");
			builder.AppendLine($"timeToStartMeasurementMs={TimeToStartMeasurementMs}");
			builder.AppendLine($"requireBarcode={RequireBarcode}");
			builder.AppendLine($"selectedWeightUnits={SelectedWeightUnits}");

			return builder.ToString();
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