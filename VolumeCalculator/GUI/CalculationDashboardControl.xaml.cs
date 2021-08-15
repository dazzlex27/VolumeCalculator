using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using VolumeCalculator.ViewModels;

namespace VolumeCalculator.GUI
{
	internal partial class CalculationDashboardControl
	{
		private CalculationDashboardControlVm _vm;

		public CalculationDashboardControl()
		{
			InitializeComponent();
		}

		private void OnCodeBoxGotFocus(object sender, KeyboardFocusChangedEventArgs e)
		{
			var vm = (CalculationDashboardControlVm)DataContext;
			vm.CodeBoxFocused = true;
		}

		private void OnCodeBoxLostFocus(object sender, KeyboardFocusChangedEventArgs e)
		{
			var vm = (CalculationDashboardControlVm)DataContext;
			vm.CodeBoxFocused = false;
		}

		private void OnUnitBoxGotFocus(object sender, KeyboardFocusChangedEventArgs e)
		{
			var vm = (CalculationDashboardControlVm)DataContext;
			vm.UnitCountBoxFocused = true;
		}

		private void OnUnitBoxLostFocus(object sender, KeyboardFocusChangedEventArgs e)
		{
			var vm = (CalculationDashboardControlVm)DataContext;
			vm.UnitCountBoxFocused = false;
		}

		private void OnCommentBoxGotFocus(object sender, KeyboardFocusChangedEventArgs e)
		{
			var vm = (CalculationDashboardControlVm)DataContext;
			vm.CommentBoxFocused = true;
		}

		private void OnCommentBoxLostFocus(object sender, RoutedEventArgs e)
		{
			var vm = (CalculationDashboardControlVm)DataContext;
			vm.CommentBoxFocused = false;
		}

		private void ButtonBase_OnClick(object sender, RoutedEventArgs e)
		{
			ResetFocus();

			var button = sender as Button;
			button?.Focus();
		}

		private void ResetFocus()
		{
			_vm.CodeBoxFocused = false;
			_vm.UnitCountBoxFocused = false;
			_vm.CommentBoxFocused = false;
			Keyboard.ClearFocus();
		}

		private void CalculationDashboardControl_OnDataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
		{
			if (!(DataContext is CalculationDashboardControlVm vm))
				return;

			_vm = vm;
		}
	}
}