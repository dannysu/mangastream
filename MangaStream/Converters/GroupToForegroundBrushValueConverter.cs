using System;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;

namespace MangaStream
{
    public class GroupToForegroundBrushValueConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            SeriesInGroup group = value as SeriesInGroup;
            object result = null;

            if (group != null)
            {
                if (group.Count == 0)
                {
                    result = (SolidColorBrush)Application.Current.Resources["PhoneDisabledBrush"];
                }
                else
                {
                    result = (SolidColorBrush)Application.Current.Resources["PhoneBackgroundBrush"];
                }
            }

            return result;
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
