using System.Text.RegularExpressions;
using System.Windows.Input;
using TextBox = System.Windows.Controls.TextBox;

namespace VolumeCalculator.GUI
{
    internal partial class MiscSettingsControl
    {
	    private readonly Regex _naturalNumberValidationRegex;
	    private readonly Regex _integerValidationRegex;

		public MiscSettingsControl()
	    {
            InitializeComponent();

		    _naturalNumberValidationRegex = new Regex("[0-9]+");
		    _integerValidationRegex = new Regex("(-)?([0-9]+)?");
		}

		private void CheckNumericTextPreview(object sender, TextCompositionEventArgs e)
	    {
		    if (!(sender is TextBox textBox))
			    return;

		    if (ReferenceEquals(textBox, TbSampleCount))
		    {
			    e.Handled = !_integerValidationRegex.IsMatch(e.Text);
				return;
		    }

		    var zeroInFirstPosition = e.Text == "0" && textBox.CaretIndex == 0;
			e.Handled = !_naturalNumberValidationRegex.IsMatch(e.Text) || zeroInFirstPosition;
	    }
	}
}