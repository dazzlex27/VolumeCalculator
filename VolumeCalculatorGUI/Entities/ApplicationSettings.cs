using System.Collections.Generic;
using System.Reflection;
using System.Windows;

namespace VolumeCalculatorGUI.Entities
{
	[Obfuscation(ApplyToMembers = false)]
	internal class ApplicationSettings
    {
	    [Obfuscation]
		public short FloorDepth { get; }

	    [Obfuscation]
		public short MinObjectHeight { get; }

	    [Obfuscation]
		public byte SampleDepthMapCount { get; }

	    [Obfuscation]
		public string OutputPath { get; }

	    [Obfuscation]
	    public bool UseColorMask { get; }

	    [Obfuscation]
	    public List<Point> ColorMaskContour { get; }

		[Obfuscation]
		public bool UseDepthMask { get; }

	    [Obfuscation]
		public List<Point> DepthMaskContour { get; }

	    public ApplicationSettings(short floorDepth, short minObjectHeight, byte sampleCount, string outputPath, 
		    bool useColorMask, IReadOnlyCollection<Point> colorMaskContour, 
		    bool useDepthMask, IReadOnlyCollection<Point> depthMaskContour)
	    {
		    FloorDepth = floorDepth > 0 ? floorDepth : (short) 1000;
		    MinObjectHeight = minObjectHeight;
		    SampleDepthMapCount = sampleCount > 0 ? sampleCount : (byte) 10;
		    OutputPath = outputPath;
		    UseColorMask = useColorMask;
		    ColorMaskContour = colorMaskContour != null ? new List<Point>(colorMaskContour) : GetDefaultAreaContour();
			UseDepthMask = useDepthMask;
		    DepthMaskContour = depthMaskContour != null ? new List<Point>(depthMaskContour) : GetDefaultAreaContour();
		}

	    [Obfuscation(Exclude = true)]
		public static ApplicationSettings GetDefaultSettings()
		{
			return new ApplicationSettings(1000, 5, 10, "C:/VolumeCalculator/", false, GetDefaultAreaContour(), 
				false, GetDefaultAreaContour());
		}

	    public override string ToString()
	    {
		    return
			    $"floorDepth={FloorDepth} useColorMask= {UseColorMask} useDepthMask={UseDepthMask} minObjHeight={MinObjectHeight} sampleCount={SampleDepthMapCount} outputPath={OutputPath}";

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