using System.Windows;
using System.Windows.Input;

namespace VolumeCalculatorGUI.GUI
{
	internal partial class CalculationDashboardControl
	{
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
			var vm = (CalculationDashboardControlVm)DataContext;
			vm.CodeBoxFocused = false;
			vm.UnitCountBoxFocused = false;
			vm.CommentBoxFocused = false;
		}
	}
}