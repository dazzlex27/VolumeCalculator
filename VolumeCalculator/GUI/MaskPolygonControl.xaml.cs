using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using VolumeCalculator.ViewModels;

namespace VolumeCalculator.GUI
{
	internal partial class MaskPolygonControl
	{
		private static readonly int NodeSize = 14;
		private static readonly int HalfNodeSize = NodeSize / 2;

		private static Color _polygonColor = Colors.Blue;

		private readonly List<Ellipse> _polygonNodes;

		private MaskPolygonControlVm _vm;

		private Point _clickPoint;
		private Shape _selectedShape;

		public static readonly DependencyProperty IsReadOnlyProperty =
			DependencyProperty.Register(nameof(IsReadOnly), typeof(bool), typeof(MaskPolygonControl));

		public bool IsReadOnly
		{
			get => (bool)GetValue(IsReadOnlyProperty);
			set
			{
				SetValue(IsReadOnlyProperty, value);
				SetPoints(value ? new List<Point>() : _vm.PolygonPoints.ToList());
			}
		}

		public static readonly DependencyProperty RectangleOnlyProperty =
			DependencyProperty.Register(nameof(RectangleOnly), typeof(bool), typeof(MaskPolygonControl));

		public bool RectangleOnly
		{
			get => (bool)GetValue(RectangleOnlyProperty);
			set
			{
				SetValue(RectangleOnlyProperty, value);
				//SetPoints(value ? new List<Point>() : _vm.PolygonPoints.ToList());
			}
		}

		public static readonly DependencyProperty MainCanvasProperty =
			DependencyProperty.Register(nameof(MainCanvas), typeof(Canvas), typeof(MaskPolygonControl));

		public Canvas MainCanvas
		{
			get => (Canvas) GetValue(MainCanvasProperty);
			set => SetValue(MainCanvasProperty, value);
		}

		public MaskPolygonControl()
		{
			_polygonNodes = new List<Ellipse>();

			AssignEllipseColor();
			InitializeComponent();
		}

		private void AssignEllipseColor()
		{
			if (_polygonColor != Colors.Blue)
				return;

			try
			{
				var mainBrushColor = Application.Current.Resources["Brush01"];
				// ReSharper disable once PossibleNullReferenceException
				_polygonColor = (Color)ColorConverter.ConvertFromString(mainBrushColor.ToString());
			}
			catch (Exception)
			{
				// ignored
			}
		}

		private IReadOnlyList<Point> GetPoints()
		{
			if (IsReadOnly)
				return null;

			var points = new List<Point>(_polygonNodes.Count);

			foreach (var ellipse in _polygonNodes)
			{
				var pointX = Canvas.GetLeft(ellipse) + HalfNodeSize;
				var pointY = Canvas.GetTop(ellipse) + HalfNodeSize;
				var point = new Point(pointX, pointY);
				points.Add(point);
			}

			return points;
		}

		private void SetPoints(IEnumerable<Point> absPoints)
		{
			if (IsReadOnly)
				return;

			foreach (var node in _polygonNodes)
				CvMain.Children.Remove(node);
			_polygonNodes.Clear();

			foreach (var point in absPoints)
			{
				var ellipse = new Ellipse
				{
					Width = NodeSize,
					Height = NodeSize,
					Stroke = new SolidColorBrush(_polygonColor),
					Fill = new SolidColorBrush(_polygonColor)
				};

				_polygonNodes.Add(ellipse);
				CvMain.Children.Add(ellipse);

				Canvas.SetLeft(ellipse, point.X - HalfNodeSize);
				Canvas.SetTop(ellipse, point.Y - HalfNodeSize);
			}
		}

		private void CvMain_MouseDown(object sender, MouseButtonEventArgs e)
		{
			if (IsReadOnly)
				return;

			foreach (var node in _polygonNodes)
			{
				if (!node.IsMouseDirectlyOver)
					continue;

				_selectedShape = node;
				break;
			}

			if (_selectedShape == null)
				return;

			_clickPoint = e.GetPosition(_selectedShape);
		}

		private void CvMain_MouseMove(object sender, MouseEventArgs e)
		{
			if (_selectedShape == null)
				return;

			var point = e.GetPosition(CvMain);
			var newPoint = SnapPointToBoundsIfNecessary(point);

			if (RectangleOnly)
				SnapPointsToRectangle(newPoint);

			Canvas.SetLeft(_selectedShape, newPoint.X - _clickPoint.X);
			Canvas.SetTop(_selectedShape, newPoint.Y - _clickPoint.Y);

			// var newPoints = GetPoints();
			// _vm.SetPolygonPoints(GetRelPoints(newPoints));
			//
			_vm.PolygonPoints = new PointCollection(GetPoints());
		}

		private void CvMain_MouseUp(object sender, MouseButtonEventArgs e)
		{
			if (IsReadOnly)
				return;
			
			var newPoints = GetPoints();
			_vm.SetPolygonPoints(GetRelPoints(newPoints));

			_selectedShape = null;
		}

		private void MaskPolygonControl_OnDataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
		{
			_vm = (MaskPolygonControlVm)DataContext;
			_vm.PolygonPointsChanged += Vm_PolygonPointsChanged;
			_vm.SetCanvasSize(CvMain.ActualWidth, CvMain.ActualHeight);
		}

		private Point SnapPointToBoundsIfNecessary(Point point)
		{
			var newPoint = new Point(point.X, point.Y);

			if (point.X < HalfNodeSize)
				newPoint.X = HalfNodeSize;
			if (point.Y < HalfNodeSize)
				newPoint.Y = HalfNodeSize;
			if (point.X > CvMain.ActualWidth - HalfNodeSize)
				newPoint.X = CvMain.ActualWidth - HalfNodeSize;
			if (point.Y > CvMain.ActualHeight - HalfNodeSize)
				newPoint.Y = CvMain.ActualHeight - HalfNodeSize;

			return newPoint;
		}

		private void SnapPointsToRectangle(Point newPoint)
		{
			if (_polygonNodes.Count != 4)
				throw new NotImplementedException("RectangleOnly mode does is not available when nodeCount <> 4");

			var index = 0;
			for (var i = 0; i < _polygonNodes.Count; i++)
			{
				if (!ReferenceEquals(_polygonNodes[i], _selectedShape))
					continue;

				index = i;
				break;
			}

			switch (index)
			{
				case 0:
					Canvas.SetLeft(_polygonNodes[1], newPoint.X - _clickPoint.X);
					Canvas.SetTop(_polygonNodes[3], newPoint.Y - _clickPoint.Y);
					break;
				case 1:
					Canvas.SetLeft(_polygonNodes[0], newPoint.X - _clickPoint.X);
					Canvas.SetTop(_polygonNodes[2], newPoint.Y - _clickPoint.Y);
					break;
				case 2:
					Canvas.SetLeft(_polygonNodes[3], newPoint.X - _clickPoint.X);
					Canvas.SetTop(_polygonNodes[1], newPoint.Y - _clickPoint.Y);
					break;
				case 3:
					Canvas.SetLeft(_polygonNodes[2], newPoint.X - _clickPoint.X);
					Canvas.SetTop(_polygonNodes[0], newPoint.Y - _clickPoint.Y);
					break;
			}
		}

		private void Vm_PolygonPointsChanged(IReadOnlyList<Point> obj)
		{
			SetPoints(_vm.PolygonPoints.ToList());
		}

		private void CvMain_OnMouseLeave(object sender, MouseEventArgs e)
		{
			if (IsReadOnly)
				return;

			_selectedShape = null;
		}

		private void MaskPolygonControl_OnSizeChanged(object sender, SizeChangedEventArgs e)
		{
			_vm?.SetCanvasSize(CvMain.ActualWidth, CvMain.ActualHeight);
		}
		
		private IReadOnlyList<Point> GetRelPoints(IReadOnlyCollection<Point> absPoints)
		{
			var relPoints = new List<Point>(absPoints.Count);

			foreach (var point in absPoints)
			{
				var relPoint = new Point(point.X / CvMain.ActualWidth, point.Y / CvMain.ActualHeight);
				relPoints.Add(relPoint);
			}

			return relPoints;
		}
	}
}