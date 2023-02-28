using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Forms;
using System.Windows.Interop;

namespace VCClient.GUI
{
    internal partial class SettingsWindow
    {
		public SettingsWindow()
	    {
            InitializeComponent();
		}

		private void BtOk_Click(object sender, RoutedEventArgs e)
		{
			var validationPassed = IsValid(UcWorkAreaSettings.TbDistanceToFloor)
									&& IsValid(UcWorkAreaSettings.TbMinObjHeight)
									&& IsValid(UcWorkAreaSettings.TbRangeMeterValue)
									&& IsValid(UcMiscSettings.TbSampleCount);
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

		private void SettingsWindow_OnSizeChanged(object sender, SizeChangedEventArgs e)
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