using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using VCClient.ViewModels;

namespace VCClient.GUI
{
	internal partial class DashboardControl
	{
		private DashboardControlVm _vm;

		public DashboardControl()
		{
			InitializeComponent();
		}

		private void OnCodeBoxGotFocus(object sender, KeyboardFocusChangedEventArgs e)
		{
			_vm.CodeBoxFocused = true;
		}

		private void OnCodeBoxLostFocus(object sender, KeyboardFocusChangedEventArgs e)
		{
			_vm.CodeBoxFocused = false;
		}

		private void OnUnitBoxGotFocus(object sender, KeyboardFocusChangedEventArgs e)
		{
			_vm.UnitCountBoxFocused = true;
		}

		private void OnUnitBoxLostFocus(object sender, KeyboardFocusChangedEventArgs e)
		{
			_vm.UnitCountBoxFocused = false;
		}

		private void OnCommentBoxGotFocus(object sender, KeyboardFocusChangedEventArgs e)
		{
			_vm.CommentBoxFocused = true;
		}

		private void OnCommentBoxLostFocus(object sender, RoutedEventArgs e)
		{
			_vm.CommentBoxFocused = false;
		}

		private void OnButtonClicked(object sender, RoutedEventArgs e)
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

		private void OnDataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
		{
			if (!(DataContext is DashboardControlVm vm))
				return;

			_vm = vm;
		}
	}
}
