using System;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Interop;
using TextBox = System.Windows.Controls.TextBox;

namespace VolumeCalculator.GUI
{
    internal partial class SettingsWindow
    {
	    private readonly Regex _naturalNumberValidationRegex;
	    private readonly Regex _integerValidationRegex;

		public SettingsWindow()
	    {
            InitializeComponent();

		    _naturalNumberValidationRegex = new Regex("[0-9]+");
		    _integerValidationRegex = new Regex("(-)?([0-9]+)?");
		}

		private void BtOk_Click(object sender, RoutedEventArgs e)
		{
			var validationPassed = IsValid(TbDistanceToFloor) && IsValid(TbMinObjHeight) && IsValid(TbSampleCount);
			if (!validationPassed)
				return;

			DialogResult = true;
			Close();
		}

		private void BtCancel_Click(object sender, RoutedEventArgs e)
		{
			DialogResult = false;
			Close();
		}

		private static bool IsValid(DependencyObject obj)
		{
			return !Validation.GetHasError(obj) && LogicalTreeHelper.GetChildren(obj).OfType<DependencyObject>().
				       All(IsValid);
		}

	    private void CheckNumericTextPreview(object sender, TextCompositionEventArgs e)
	    {
		    if (!(sender is TextBox textBox))
			    return;

		    if (ReferenceEquals(textBox, TbMinObjHeight))
		    {
			    e.Handled = !_integerValidationRegex.IsMatch(e.Text);
				return;
		    }

		    var zeroInFirstPosition = e.Text == "0" && textBox.CaretIndex == 0;
			e.Handled = !_naturalNumberValidationRegex.IsMatch(e.Text) || zeroInFirstPosition;
	    }

	    private void SettingsWindow_OnSizeChanged(object sender, SizeChangedEventArgs e)
	    {
		    if (!(sender is Window window))
			    return;

		    var currentScreenHandle = new WindowInteropHelper(window).Handle;
			var currentScreem = Screen.FromHandle(currentScreenHandle);

		    var source = PresentationSource.FromVisual(window);
		    var dpiScaling = source?.CompositionTarget?.TransformFromDevice.M11 ?? 1;

		    var workingArea = currentScreem.WorkingArea;
		    var workAreaWidth = (int)Math.Floor(workingArea.Width * dpiScaling);
		    var workAreaHeight = (int)Math.Floor(workingArea.Height * dpiScaling);

		    window.Left = (workAreaWidth - e.NewSize.Width * dpiScaling) / 2 + workingArea.Left * dpiScaling;
		    window.Top = (workAreaHeight - e.NewSize.Height * dpiScaling) / 2 + workingArea.Top * dpiScaling;
		}
    }
}