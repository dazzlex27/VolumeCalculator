using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;

namespace VolumeCalculatorGUI.GUI
{
	internal partial class MaskPolygonControl : UserControl
	{
		private static readonly int NodeSize = 14;
		private static readonly int HalfNodeSize = NodeSize / 2;
		private static readonly Color PolygonColor = Colors.ForestGreen;

		private readonly List<Ellipse> _polygonNodes;

		private MaskPolygonControlVm _vm;

		private Point _clickPoint;
		private Shape _selectedShape;

		public MaskPolygonControl()
		{
			_polygonNodes = new List<Ellipse>();

			InitializeComponent();
		}

		private IEnumerable<Point> GetPoints()
		{
			var points = new List<Point>(_polygonNodes.Count);

			foreach (var ellipse in _polygonNodes)
			{
				var pointX = (Canvas.GetLeft(ellipse) + HalfNodeSize);
				var pointY = (Canvas.GetTop(ellipse) + HalfNodeSize);
				var point = new Point(pointX, pointY);
				points.Add(point);
			}

			return points;
		}

		private void SetPoints(IEnumerable<Point> absPoints)
		{
			_polygonNodes.Clear();

			foreach (var point in absPoints)
			{
				var ellipse = new Ellipse
				{
					Width = NodeSize,
					Height = NodeSize,
					Stroke = new SolidColorBrush(PolygonColor),
					Fill = new SolidColorBrush(PolygonColor)
				};

				_polygonNodes.Add(ellipse);
				CvMain.Children.Add(ellipse);

				Canvas.SetLeft(ellipse, point.X - HalfNodeSize);
				Canvas.SetTop(ellipse, point.Y - HalfNodeSize);
			}
		}

		private void UpdateSettingsIfNeeded()
		{
			if (_selectedShape == null)
				return;

			_vm.RaisePolygonPointsChangedEvent();
		}

		private void CvMain_MouseDown(object sender, MouseButtonEventArgs e)
		{
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

			var newPoint = e.GetPosition(CvMain);
			if (newPoint.X < HalfNodeSize)
				newPoint.X = HalfNodeSize;
			if (newPoint.Y < HalfNodeSize)
				newPoint.Y = HalfNodeSize;
			if (newPoint.X > CvMain.ActualWidth - HalfNodeSize)
				newPoint.X = CvMain.ActualWidth - HalfNodeSize;
			if (newPoint.Y > CvMain.ActualHeight - HalfNodeSize)
				newPoint.Y = CvMain.ActualHeight - HalfNodeSize;

			Canvas.SetLeft(_selectedShape, newPoint.X - _clickPoint.X);
			Canvas.SetTop(_selectedShape, newPoint.Y - _clickPoint.Y);

			_vm.PolygonPoints = new PointCollection(GetPoints());
		}

		private void CvMain_MouseUp(object sender, MouseButtonEventArgs e)
		{
			UpdateSettingsIfNeeded();
			_selectedShape = null;
		}

		private void MaskPolygonControl_OnDataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
		{
			_vm = (MaskPolygonControlVm)DataContext;
			SetPoints(_vm.PolygonPoints.ToList());
			_vm.AbsPolygonPointsUpdated += Vm_PolygonPointUpdated;
		}

		private void CvMain_OnMouseLeave(object sender, MouseEventArgs e)
		{
			UpdateSettingsIfNeeded();
			_selectedShape = null;
		}

		private void Vm_PolygonPointUpdated(IReadOnlyList<Point> absPoints)
		{
			SetPoints(absPoints);
		}
	}
}