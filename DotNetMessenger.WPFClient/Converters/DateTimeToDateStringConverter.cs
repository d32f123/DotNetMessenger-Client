using System;
using System.Windows.Data;

namespace DotNetMessenger.WPFClient.Converters
{
    public class DateTimeToDateStringConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if (!(value is DateTime dateTime)) return null;
            return dateTime == DateTime.MinValue
                ? string.Empty
                : dateTime.ToString(dateTime.Date.Equals(DateTime.Now.Date) ? "T" : "g");
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            switch (value)
            {
                case null:
                    return null;
                case string s:
                    try
                    {
                        return DateTime.Parse(s);
                    }
                    catch
                    {
                        return null;
                    }
            }
            return null;
        }
    }
}