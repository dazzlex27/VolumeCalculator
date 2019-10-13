using System;
using System.ComponentModel;
using System.Globalization;
using System.Windows.Data;

namespace GuiCommon.Converters
{
	public class EnumDescriptionConverter : IValueConverter
	{
		public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
		{
			return value is Enum enumValue ? GetEnumDescription(enumValue) : null;
		}

		public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
		{
			return string.Empty;
		}

		private static string GetEnumDescription(Enum enumObject)
		{
			var enumType = enumObject.GetType();
			var field = enumType.GetField(enumObject.ToString());
			var attributes = field.GetCustomAttributes(typeof(DescriptionAttribute),
				false);
			return attributes.Length == 0
				? enumObject.ToString()
				: ((DescriptionAttribute)attributes[0]).Description;
		}
	}
}