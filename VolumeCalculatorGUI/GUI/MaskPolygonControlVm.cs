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
		public event Action<IReadOnlyList<Point>> AbsPolygonPointsUpdated;

		private static readonly int NodeSize = 4;
		private static readonly Color PolygonColor = Colors.DarkGreen;

		private bool _dragging;

		private double _canvasWidth;
		private double _canvasHeight;

		private PointCollection _polygonPoints;

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

		public double CanvasWidth => 480;
		//{
		//	get => _canvasWidth;
		//	set
		//	{
		//		if (_canvasWidth == value)
		//			return;

		//		_canvasWidth = value;
		//		OnPropertyChanged();
		//	}
		//}

		public double CanvasHeight => 360;
		//{
		//	get => _canvasHeight;
		//	set
		//	{
		//		if (_canvasHeight == value)
		//			return;

		//		_canvasHeight = value;
		//		OnPropertyChanged();
		//	}
		//}

		//public double CanvasWidth
		//{
		//	get => (double)GetValue(CanvasWidthProperty);
		//	set => SetValue(CanvasWidthProperty, value);
		//}

		//public static readonly DependencyProperty CanvasWidthProperty =
		//	DependencyProperty.Register(nameof(CanvasWidth), typeof(double), typeof(MaskPolygonControl));

		//public double CanvasHeight
		//{
		//	get => (double)GetValue(CanvasHeightProperty);
		//	set => SetValue(CanvasHeightProperty, value);
		//}

		//public static readonly DependencyProperty CanvasHeightProperty =
		//	DependencyProperty.Register(nameof(CanvasHeight), typeof(double), typeof(MaskPolygonControl));

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

		public void RaisePolygonPointsChangedEvent()
		{
			var relPoints = GetRelPoints(_polygonPoints.ToList());
			PolygonPointsChanged?.Invoke(relPoints);
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