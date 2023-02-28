using Primitives;
using System.Windows;

namespace VCClient.Utils
{
	internal static class RelPointExtensions
	{
		public static RelPoint ConvertToRelPoint(this Point point)
		{
			return new RelPoint(point.X, point.Y);
		}

		public static Point ConvertToPoint(this RelPoint point)
		{
			return new Point(point.X, point.Y);
		}
	}
}
