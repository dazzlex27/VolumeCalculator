using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace GuiCommon.Converters
{
	[ValueConversion(typeof(bool), typeof(bool))]
	public class InverseBooleanToVisibilityConverter : IValueConverter
	{
		public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
		{
			if (targetType != typeof(Visibility))
				throw new InvalidOperationException("The target must be a Visibility");

			var boolValue = value != null && (bool) value;

			return boolValue ? Visibility.Collapsed : Visibility.Visible;
		}

		public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
		{
			throw new NotSupportedException();
		}
	}
}