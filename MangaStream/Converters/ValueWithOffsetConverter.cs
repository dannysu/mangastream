using System;
using System.Windows.Data;
using System.Globalization;

namespace MangaStream
{
    public class ValueWithOffsetConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            int result = System.Convert.ToInt32(value, CultureInfo.InvariantCulture);
            int offset = System.Convert.ToInt32(parameter, CultureInfo.InvariantCulture);

            return result + offset;
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
