using System.Collections.Generic;
using System.IO;
using System.Linq;
using Common;
using Point = System.Windows.Point;

namespace VolumeCalculatorGUI.Utils
{
	internal class GeometryUtils
	{
		public static bool[] CreateWorkingAreaMask(IReadOnlyList<Point> polygon, int width, int height)
		{
			var absPoints = polygon.Select(p => new Point(p.X * width, p.Y * height)).ToList();

			var polygonMask = new bool[width * height];

			var index = 0;
			for (var j = 0; j < height; j++)
			{
				for (var i = 0; i < width; i++)
				{
					polygonMask[index] = IsPointInsidePolygon(absPoints, i, j);
					index++;
				}
			}

			return polygonMask;
		}

		public static void ApplyWorkingAreaMask(DepthMap depthMap, bool[] mask)
		{
			if (depthMap.Data.Length != mask.Length)
				throw new InvalidDataException("Depth map data length is different from the mask's length");

			for (var i = 0; i < depthMap.Data.Length; i++)
			{
				if (!mask[i])
					depthMap.Data[i] = 0;
			}
		}

		public static List<Point> GetRectangleFromZonePolygon(IReadOnlyList<Point> polygonPoints)
		{
			var smallestX = double.MaxValue;
			var smallestY = double.MaxValue;
			var largestX = double.MinValue;
			var largestY = double.MinValue;

			var rectanglePoints = new List<Point>();

			foreach (var point in polygonPoints)
			{
				if (point.X < smallestX)
					smallestX = point.X;
				if (point.X > largestX)
					largestX = point.X;
				if (point.Y < smallestY)
					smallestY = point.Y;
				if (point.Y > largestY)
					largestY = point.Y;
			}

			rectanglePoints.Add(new Point(smallestX, smallestY));
			rectanglePoints.Add(new Point(largestX, smallestY));
			rectanglePoints.Add(new Point(largestX, largestY));
			rectanglePoints.Add(new Point(smallestX, largestY));

			return rectanglePoints;
		}

		/// <summary>
		///   Source:  https://wrf.ecse.rpi.edu//Research/Short_Notes/pnpoly.html
		/// </summary>
		private static bool IsPointInsidePolygon(IReadOnlyList<Point> polygon, int column, int row)
		{
			var pointIsInPolygon = false;

			for (int i = 0, j = polygon.Count - 1; i < polygon.Count; j = i++)
			{
				var pointInLineScope = (polygon[i].Y > row) != (polygon[j].Y > row);

				var lineXHalf = (polygon[j].X - polygon[i].X) * (row - polygon[i].Y) / (polygon[j].Y - polygon[i].Y);
				var lineXPosition = lineXHalf + polygon[i].X;
				var pointInLeftHalfPlaneOfLine = column < lineXPosition;

				if (pointInLineScope && pointInLeftHalfPlaneOfLine)
					pointIsInPolygon = !pointIsInPolygon;
			}

			return pointIsInPolygon;
		}
	}
}