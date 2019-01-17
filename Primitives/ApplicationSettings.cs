using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Runtime.Serialization;
using System.Windows;

namespace Primitives
{
	[ObfuscationAttribute(Exclude = false, Feature = "rename")]
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

		public WebRequestSettings WebRequestSettings { get; set; }

	    public SqlRequestSettings SqlRequestSettings { get; set; }

		public bool RunInFullSreen { get; set; }

	    public string ResultsFilePath => Path.Combine(OutputPath, "results.csv");

	    public string PhotosDirectoryPath => Path.Combine(OutputPath, "photos");

	    public ApplicationSettings(short floorDepth, short minObjectHeight, byte sampleCount, string outputPath, 
		    bool useColorMask, IReadOnlyCollection<Point> colorMaskContour, 
		    bool useDepthMask, IReadOnlyCollection<Point> depthMaskContour,
		    long timeToStartMeasurementMs, bool useRgbAlgorithmByDefault, WebRequestSettings webRequestSettings, 
		    SqlRequestSettings sqlRequestSettings)
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
		    WebRequestSettings = webRequestSettings;
		    SqlRequestSettings = sqlRequestSettings;
	    }

	    [Obfuscation(Exclude = true)]
	    public static ApplicationSettings GetDefaultSettings()
	    {
		    return new ApplicationSettings(1000, 5, 10, "MeasurementResults", false, GetDefaultAreaContour(),
			    false, GetDefaultAreaContour(), 5000, false, WebRequestSettings.GetDefaultSettings(),
			    SqlRequestSettings.GetDefaultSettings());
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

			if (WebRequestSettings == null)
				WebRequestSettings = WebRequestSettings.GetDefaultSettings();

			if (SqlRequestSettings == null)
				SqlRequestSettings = SqlRequestSettings.GetDefaultSettings();
	    }
    }
}