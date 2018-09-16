using System.Linq;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace VolumeCalculatorGUI.GUI
{
    internal partial class SettingsWindow
    {
	    private readonly Regex _numericValidationRegex;
	    private readonly Regex _numericNonZeroValidationRegex;

		public SettingsWindow()
	    {
            InitializeComponent();

		    _numericValidationRegex = new Regex("[0-9]+");
		    _numericNonZeroValidationRegex = new Regex("[1-9]([0-9]+)?");
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
			    e.Handled = !_numericValidationRegex.IsMatch(e.Text);
				return;
		    }

		    e.Handled = !_numericNonZeroValidationRegex.IsMatch(e.Text);
	    }
	}
}