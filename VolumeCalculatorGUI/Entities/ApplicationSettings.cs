using System.Collections.Generic;
using System.Windows;

namespace VolumeCalculatorGUI.Entities
{
    internal class ApplicationSettings
    {
        public short DistanceToFloor { get; }

        public short MinObjHeight { get; }

		public string OutputPath { get; }

		public bool UseWorkingAreaMask { get; }

		public List<Point> WorkingAreaContour { get; }

	    public ApplicationSettings(short distanceToFloor, short minObjHeight, string outputPath, bool useWorkingAreaMask, 
		    List<Point> workingAreaContour)
	    {
		    DistanceToFloor = distanceToFloor;
		    MinObjHeight = minObjHeight;
		    OutputPath = outputPath;
		    UseWorkingAreaMask = useWorkingAreaMask;
		    WorkingAreaContour = workingAreaContour ?? GetDefaultAreaContour();
	    }

		public static ApplicationSettings GetDefaultSettings()
		{
			return new ApplicationSettings(1000, 995, "C:/VolumeChecker/", false, GetDefaultAreaContour());
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