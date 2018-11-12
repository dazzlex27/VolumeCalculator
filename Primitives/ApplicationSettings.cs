using System.Collections.Generic;
using System.Reflection;
using System.Windows;

namespace Primitives
{
	[Obfuscation(ApplyToMembers = false)]
	public class ApplicationSettings
    {
	    [Obfuscation]
		public short FloorDepth { get; set; }

	    [Obfuscation]
		public short MinObjectHeight { get; set; }

	    [Obfuscation]
		public byte SampleDepthMapCount { get; set; }

	    [Obfuscation]
		public string OutputPath { get; set; }

	    [Obfuscation]
	    public bool UseColorMask { get; set; }

	    [Obfuscation]
	    public List<Point> ColorMaskContour { get; set; }

		[Obfuscation]
		public bool UseDepthMask { get; set; }

	    [Obfuscation]
		public List<Point> DepthMaskContour { get; set; }

		[Obfuscation]
		public long TimeToStartMeasurementMs { get; set; }

		[Obfuscation]
		public bool UseRgbAlgorithmByDefault { get; set; }

	    public ApplicationSettings(short floorDepth, short minObjectHeight, byte sampleCount, string outputPath, 
		    bool useColorMask, IReadOnlyCollection<Point> colorMaskContour, 
		    bool useDepthMask, IReadOnlyCollection<Point> depthMaskContour,
		    long timeToStartMeasurementMs, bool useRgbAlgorithmByDefault)
	    {
		    FloorDepth = floorDepth > 0 ? floorDepth : (short) 1000;
		    MinObjectHeight = minObjectHeight;
		    SampleDepthMapCount = sampleCount > 0 ? sampleCount : (byte) 10;
		    OutputPath = outputPath;
		    UseColorMask = useColorMask;
		    ColorMaskContour = colorMaskContour != null ? new List<Point>(colorMaskContour) : GetDefaultAreaContour();
			UseDepthMask = useDepthMask;
		    DepthMaskContour = depthMaskContour != null ? new List<Point>(depthMaskContour) : GetDefaultAreaContour();
		    TimeToStartMeasurementMs = timeToStartMeasurementMs;
		    UseRgbAlgorithmByDefault = useRgbAlgorithmByDefault;
	    }

	    [Obfuscation(Exclude = true)]
	    public static ApplicationSettings GetDefaultSettings()
	    {
		    return new ApplicationSettings(1000, 5, 10, "MeasurementResults", false, GetDefaultAreaContour(),
			    false, GetDefaultAreaContour(), 5000, false);
	    }

	    public override string ToString()
	    {
		    return $"floorDepth={FloorDepth} useColorMask={UseColorMask} useDepthMask={UseDepthMask} minObjHeight={MinObjectHeight} sampleCount={SampleDepthMapCount} outputPath={OutputPath}";

	    }

	    [Obfuscation(Exclude = true)]
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