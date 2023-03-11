using System.Windows.Input;
using ProcessingUtils;
using TextBox = System.Windows.Controls.TextBox;

namespace VCClient.GUI
{
	internal partial class MiscSettingsControl
	{
		public MiscSettingsControl()
		{
			InitializeComponent();
		}

		private void CheckNumericTextPreview(object sender, TextCompositionEventArgs e)
		{
			if (!(sender is TextBox textBox))
				return;

			if (ReferenceEquals(textBox, TbSampleCount))
			{
				e.Handled = !RegexInstances.IntegerValidator.IsMatch(e.Text);
				return;
			}

			var zeroInFirstPosition = e.Text == "0" && textBox.CaretIndex == 0;
			e.Handled = !RegexInstances.NaturalNumberValidator.IsMatch(e.Text) || zeroInFirstPosition;
		}
	}
}
