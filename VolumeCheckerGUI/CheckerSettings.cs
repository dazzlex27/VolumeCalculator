namespace VolumeCheckerGUI
{
    internal class CheckerSettings
    {
        public short DistanceToFloor { get; }

        public short MinObjHeight { get; }

        public CheckerSettings(short distanceToFloor, short minObjHeight)
        {
			DistanceToFloor = distanceToFloor;
			MinObjHeight = minObjHeight;
        }

		public static CheckerSettings GetDefaultSettings()
		{
			return new CheckerSettings(2000, 1900);
		}
    }
}