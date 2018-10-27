using System.Collections.Generic;
using System.Reflection;
using System.Windows;

namespace VolumeCalculatorGUI.Entities
{
	[Obfuscation(ApplyToMembers = false)]
	internal class ApplicationSettings
    {
	    [Obfuscation]
		public short DistanceToFloor { get; }

	    [Obfuscation]
		public short MinObjHeight { get; }

	    [Obfuscation]
		public byte SampleCount { get; }

	    [Obfuscation]
		public string OutputPath { get; }

	    [Obfuscation]
	    public bool UseColorMask { get; }

	    [Obfuscation]
	    public List<Point> ColorAreaContour { get; }

		[Obfuscation]
		public bool UseDepthMask { get; }

	    [Obfuscation]
		public List<Point> DepthAreaContour { get; }

	    public ApplicationSettings(short distanceToFloor, short minObjHeight, byte sampleCount, string outputPath, 
		    bool useColorMask, IReadOnlyCollection<Point> colorAreaContour, 
		    bool useDepthMask, IReadOnlyCollection<Point> depthAreaContour)
	    {
		    DistanceToFloor = distanceToFloor > 0 ? distanceToFloor : (short) 1000;
		    MinObjHeight = minObjHeight;
		    SampleCount = sampleCount > 0 ? sampleCount : (byte) 10;
		    OutputPath = outputPath;
		    UseColorMask = useColorMask;
		    ColorAreaContour = colorAreaContour != null ? new List<Point>(colorAreaContour) : GetDefaultAreaContour();
			UseDepthMask = useDepthMask;
		    DepthAreaContour = depthAreaContour != null ? new List<Point>(depthAreaContour) : GetDefaultAreaContour();
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
			    $"floorDepth={DistanceToFloor} useColorMask= {UseColorMask} useDepthMask={UseDepthMask} minObjHeight={MinObjHeight} sampleCount={SampleCount} outputPath={OutputPath}";

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