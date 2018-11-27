using System;
using System.Globalization;
using System.Windows.Data;

namespace VolumeCalculatorGUI.GUI.Utils
{
    internal class HalfMeasureConverter : IValueConverter
    {
	    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
	    {
		    return (double) value / 2.0;
	    }

	    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
	    {
		    return (double) value * 2.0;
	    }
    }
}