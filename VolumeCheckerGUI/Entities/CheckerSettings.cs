namespace VolumeCheckerGUI.Entities
{
    internal class ApplicationSettings
    {
        public short DistanceToFloor { get; }

        public short MinObjHeight { get; }

		public string OutputPath { get; }

	    public ApplicationSettings(short distanceToFloor, short minObjHeight, string outputPath)
	    {
		    DistanceToFloor = distanceToFloor;
		    MinObjHeight = minObjHeight;
		    OutputPath = outputPath;
	    }

		public static ApplicationSettings GetDefaultSettings()
		{
			return new ApplicationSettings(1000, 950, "c:/VolumeChecker/");
		}
    }
}