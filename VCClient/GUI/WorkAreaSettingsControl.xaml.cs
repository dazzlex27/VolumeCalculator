using System;
using System.Windows;
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Interop;
using VCClient.Utils;
using TextBox = System.Windows.Controls.TextBox;

namespace VCClient.GUI
{
	internal partial class WorkAreaSettingsControl
	{
		public WorkAreaSettingsControl()
		{
			InitializeComponent();
		}

		private void CheckNumericTextPreview(object sender, TextCompositionEventArgs e)
		{
			if (!(sender is TextBox textBox))
				return;

			if (ReferenceEquals(textBox, TbMinObjHeight))
			{
			    e.Handled = !RegexInstances.IntegerValidator.IsMatch(e.Text);
				return;
			}

			var zeroInFirstPosition = e.Text == "0" && textBox.CaretIndex == 0;
			e.Handled = !RegexInstances.NaturalNumberValidator.IsMatch(e.Text) || zeroInFirstPosition;
		}

		private void OnSettingsWindowSizeChanged(object sender, SizeChangedEventArgs e)
		{
			if (!(sender is Window window))
				return;

			var source = PresentationSource.FromVisual(window);
			var dpiScaling = source?.CompositionTarget?.TransformFromDevice.M11 ?? 1;

			var currentScreenHandle = new WindowInteropHelper(window).Handle;
			var currentScreen = Screen.FromHandle(currentScreenHandle);
			var workingArea = currentScreen.WorkingArea;
			var workAreaWidth = (int)Math.Floor(workingArea.Width * dpiScaling);
			var workAreaHeight = (int)Math.Floor(workingArea.Height * dpiScaling);

			window.Left = (workAreaWidth - e.NewSize.Width * dpiScaling) / 2 + workingArea.Left * dpiScaling;
			window.Top = (workAreaHeight - e.NewSize.Height * dpiScaling) / 2 + workingArea.Top * dpiScaling;
		}
	}
}
