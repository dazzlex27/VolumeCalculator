using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Text;

namespace Primitives.Settings
{
	public class WorkAreaSettings
	{
		private const int DefaultFloorDepthMm = 1805;
		private const int DefaultMinObjHeightMm = 15;

		public short FloorDepth { get; set; }

		public short MinObjectHeight { get; set; }

		public bool UseColorMask { get; set; }

		public IReadOnlyList<RelPoint> ColorMaskContour { get; set; }

		public bool UseDepthMask { get; set; }

		public IReadOnlyList<RelPoint> DepthMaskContour { get; set; }

		public bool EnableDmAlgorithm { get; set; }

		public bool EnablePerspectiveDmAlgorithm { get; set; }

		public bool EnableRgbAlgorithm { get; set; }
		
		public int RangeMeterCorrectionValueMm { get; set; }

		public WorkAreaSettings(short floorDepth, short minObjectHeight, bool useColorMask, IReadOnlyList<RelPoint> colorMaskContour,
			bool useDepthMask, IReadOnlyList<RelPoint> depthMaskContour, int rangeMeterCorrectionValueMm)
		{
			FloorDepth = floorDepth;
			MinObjectHeight = minObjectHeight;
			UseColorMask = useColorMask;
			ColorMaskContour = colorMaskContour;
			UseDepthMask = useDepthMask;
			DepthMaskContour = depthMaskContour;
			RangeMeterCorrectionValueMm = rangeMeterCorrectionValueMm;
			EnableDmAlgorithm = true;
			EnablePerspectiveDmAlgorithm = true;
			EnableRgbAlgorithm = true;
		}

		public override string ToString()
		{
			var builder = new StringBuilder("WorkAreaSettings:");
			builder.Append($"FloorDepth={FloorDepth}");
			builder.Append($",UseColorMask={UseColorMask}");
			builder.Append($",UseDepthMask={UseDepthMask}");
			builder.Append($",MinObjectHeight={MinObjectHeight}");
			builder.Append($",RangeMeterCorrectionValueMm={RangeMeterCorrectionValueMm}");
			builder.Append($",EnableDmAlgorithm={EnableDmAlgorithm}");
			builder.Append($",EnablePerspectiveDmAlgorithm={EnablePerspectiveDmAlgorithm}");
			builder.Append($",EnableRgbAlgorithm={EnableRgbAlgorithm}");

			return builder.ToString();
		}

		[OnDeserialized]
		private void OnDeserialized(StreamingContext context)
		{
			if (FloorDepth <= 0)
				FloorDepth = DefaultFloorDepthMm;

			if (MinObjectHeight <= 0)
				MinObjectHeight = DefaultMinObjHeightMm;

			if (ColorMaskContour == null)
				ColorMaskContour = GetDefaultAreaContour();

			if (DepthMaskContour == null)
				DepthMaskContour = GetDefaultAreaContour();
		}

		public static WorkAreaSettings GetDefaultSettings()
		{
			return new WorkAreaSettings(DefaultFloorDepthMm, DefaultMinObjHeightMm, false,
				GetDefaultAreaContour(), true, GetDefaultAreaContour(), 0);
		}

		private static List<RelPoint> GetDefaultAreaContour()
		{
			return new List<RelPoint>
			{
				new RelPoint(0.2, 0.2),
				new RelPoint(0.2, 0.8),
				new RelPoint(0.8, 0.8),
				new RelPoint(0.8, 0.2)
			};
		}
	}
}
