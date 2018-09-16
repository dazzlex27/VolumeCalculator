using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Media;
using System.Windows.Shapes;
using VolumeCalculatorGUI.GUI.Utils;

namespace VolumeCalculatorGUI.GUI
{
	internal class MaskPolygonControlVm : BaseViewModel
	{
		public event Action<IReadOnlyList<Point>> PolygonPointsChanged;

		private PointCollection _polygonPoints;

		private bool _canEditPolygon;

		private double _canvasWidth;
		private double _canvasHeight;

		public List<Ellipse> PolygonNodes { get; }

		public PointCollection PolygonPoints
		{
			get => _polygonPoints;
			set
			{
				if (_polygonPoints == value)
					return;

				_polygonPoints = value;
				OnPropertyChanged();
			}
		}

		public bool CanEditPolygon
		{
			get => _canEditPolygon;
			set
			{
				if (_canEditPolygon == value)
					return;

				_canEditPolygon = value;
				OnPropertyChanged();
			}
		}

		public double CanvasWidth
		{
			get => _canvasWidth;
			set
			{
				if (_canvasWidth == value)
					return;

				_canvasWidth = value;
				OnPropertyChanged();
			}
		}

		public double CanvasHeight
		{
			get => _canvasHeight;
			set
			{
				if (_canvasHeight == value)
					return;

				_canvasHeight = value;
				OnPropertyChanged();
			}
		}

		public MaskPolygonControlVm(IReadOnlyList<Point> points)
		{
			PolygonNodes = new List<Ellipse>();

			SetPolygonPoints(points);
		}

		public void SetPolygonPoints(IReadOnlyList<Point> relPoints)
		{
			var absPoints = GetAbsPoints(relPoints);
			PolygonPoints = new PointCollection(absPoints);
			PolygonPointsChanged?.Invoke(absPoints);
		}

		public IReadOnlyList<Point> GetPolygonPoints()
		{
			return GetRelPoints(_polygonPoints.ToList());
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