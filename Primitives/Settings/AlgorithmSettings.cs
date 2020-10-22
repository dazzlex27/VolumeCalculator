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
		
		public bool EnablePalletSubtraction { get; set; }
		
		public double PalletWeightGr { get; set; }
		
		public int PalletHeightMm { get; set; }

		public AlgorithmSettings(short floorDepth, short minObjectHeight, byte sampleDepthMapCount, bool useColorMask,
			IEnumerable<Point> colorMaskContour, bool useDepthMask, IEnumerable<Point> depthMaskContour,
			bool enableAutoTimer, long timeToStartMeasurementMs, bool requireBarcode, WeightUnits selectedWeightUnits, 
			bool enablePalletSubtraction, double palletWeightGr, int palletHeightMm)
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
			PalletWeightGr = palletWeightGr;
			PalletHeightMm = palletHeightMm;
			EnablePalletSubtraction = enablePalletSubtraction;
		}

		public static AlgorithmSettings GetDefaultSettings()
		{
			return new AlgorithmSettings(DefaultFloorDepthMm, DefaultMinObjHeightMm, DefaultSampleCount, false,
				GetDefaultAreaContour(), true, GetDefaultAreaContour(), true,
				DefaultTimerValueMs, true, WeightUnits.Gr, false, 0, 0);
		}

		public override string ToString()
		{
			var builder = new StringBuilder("AlgorithmSettings:");
			builder.Append($"floorDepth={FloorDepth}");
			builder.Append($",useColorMask={UseColorMask}");
			builder.Append($",useDepthMask={UseDepthMask}");
			builder.Append($",minObjHeight={MinObjectHeight}");
			builder.Append($",sampleCount={SampleDepthMapCount}");
			builder.Append($",enableAutTimer={EnableAutoTimer}");
			builder.Append($",timeToStartMeasurementMs={TimeToStartMeasurementMs}");
			builder.Append($",requireBarcode={RequireBarcode}");
			builder.Append($",selectedWeightUnits={SelectedWeightUnits}");
			builder.Append($",enablePalletSubtraction={EnablePalletSubtraction}");
			builder.Append($",palletWeightKg={PalletWeightGr}");
			builder.Append($",palletHeightMm={PalletHeightMm}");

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