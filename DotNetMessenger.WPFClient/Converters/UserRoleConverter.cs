using System;
using System.Globalization;
using System.Windows.Data;
using DotNetMessenger.Model.Enums;

namespace DotNetMessenger.WPFClient.Converters
{
    public class UserRoleConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null) return string.Empty;

            var key = ((Enum)value).ToString();

            return key;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (!(value is string str)) throw new ArgumentException();

            var content = (UserRoles) Enum.Parse(typeof(UserRoles), str);

            return content;
        }
    }
}