using System;
using System.Windows.Data;
using System.Globalization;

namespace MangaStream
{
    public class InverseBooleanConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            bool result = System.Convert.ToBoolean(value, CultureInfo.InvariantCulture);

            return !result;
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
