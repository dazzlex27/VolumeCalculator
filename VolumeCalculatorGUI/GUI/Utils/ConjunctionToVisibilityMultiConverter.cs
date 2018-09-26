using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace VolumeCalculatorGUI.GUI.Utils
{
    internal class ConjunctionToVisibilityMultiConverter : IMultiValueConverter
    {
	    public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
	    {
		    if (targetType != typeof(Visibility))
			    throw new InvalidOperationException($@"The target must be a {typeof(Visibility)}");

		    foreach (var value in values)
		    {
			    var property = (bool) value;
			    if (!property)
				    return Visibility.Collapsed;
		    }

		    return Visibility.Visible;
	    }

	    public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
	    {
		    throw new NotImplementedException();
	    }
    }
}