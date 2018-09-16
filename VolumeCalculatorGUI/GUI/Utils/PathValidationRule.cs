using System;
using System.Globalization;
using System.IO;
using System.Windows.Controls;

namespace VolumeCalculatorGUI.GUI.Utils
{
	internal class PathValidationRule : ValidationRule
	{
		public override ValidationResult Validate(object value, CultureInfo cultureInfo)
		{
			if (value == null)
				return new ValidationResult(false, "");

			var path = value.ToString();
			var pathIsValid = true;

			try
			{
				pathIsValid = Path.IsPathRooted(path);
			}
			catch (Exception)
			{
				pathIsValid = false;
			}

			return new ValidationResult(pathIsValid, "");
		}
	}
}