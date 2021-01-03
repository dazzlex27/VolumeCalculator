using GuiCommon;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Media;
using System.Windows.Shapes;

namespace VolumeCalculator
{
	internal class MaskPolygonControlVm : BaseViewModel
	{
		public event Action<IReadOnlyList<Point>> PolygonPointsChanged;

		private PointCollection _polygonPoints;

		private bool _canEditPolygon;

		private double _canvasWidth;
		private double _canvasHeight;

		private IReadOnlyList<Point> _relPoints;

		public List<Ellipse> PolygonNodes { get; }

		public PointCollection PolygonPoints
		{
			get => _polygonPoints;
			set => SetField(ref _polygonPoints, value, nameof(PolygonPoints));
		}

		public bool CanEditPolygon
		{
			get => _canEditPolygon;
			set => SetField(ref _canEditPolygon, value, nameof(CanEditPolygon));
		}

		public double CanvasWidth
		{
			get => _canvasWidth;
			set => SetField(ref _canvasWidth, value, nameof(CanvasWidth));
		}

		public double CanvasHeight
		{
			get => _canvasHeight;
			set => SetField(ref _canvasHeight, value, nameof(CanvasHeight));
		}

		public MaskPolygonControlVm(IReadOnlyList<Point> points)
		{
			PolygonNodes = new List<Ellipse>();

			SetPolygonPoints(points);
		}

		public void SetPolygonPoints(IReadOnlyList<Point> relPoints)
		{
			_relPoints = new List<Point>(relPoints);
			var absPoints = GetAbsPoints(relPoints);
			PolygonPoints = new PointCollection(absPoints);
			PolygonPointsChanged?.Invoke(absPoints);
		}

		public IReadOnlyList<Point> GetPolygonPoints()
		{
			return _relPoints; 
				//GetRelPoints(_polygonPoints.ToList());
		}

		public void SetCanvasSize(double width, double height)
		{
			_canvasWidth = width;
			_canvasHeight = height;
			SetPolygonPoints(_relPoints);
		}

		private IReadOnlyList<Point> GetAbsPoints(IReadOnlyCollection<Point> relPoints)
		{
			var absPoints = new List<Point>(relPoints.Count);

			foreach (var point in relPoints)
			{
				var absPoint = new Point(point.X * CanvasWidth, point.Y * CanvasHeight);
				absPoints.Add(absPoint);
			}

			return absPoints;
		}

		private IReadOnlyList<Point> GetRelPoints(IReadOnlyCollection<Point> absPoints)
		{
			var relPoints = new List<Point>(absPoints.Count);

			foreach (var point in absPoints)
			{
				var relPoint = new Point(point.X / CanvasWidth, point.Y / CanvasHeight);
				relPoints.Add(relPoint);
			}

			return relPoints;
		}
	}
}