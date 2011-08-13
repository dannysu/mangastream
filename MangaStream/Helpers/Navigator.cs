using System;
using System.Windows;
using System.Windows.Controls;

namespace MangaStream
{
    public static class Navigator
    {
        public static INavigable GetSource(DependencyObject obj)
        {
            return (INavigable)obj.GetValue(SourceProperty);
        }

        public static void SetSource(DependencyObject obj, INavigable value)
        {
            obj.SetValue(SourceProperty, value);
        }

        public static readonly DependencyProperty SourceProperty =
            DependencyProperty.RegisterAttached("Source", typeof(INavigable), typeof(Navigator), new PropertyMetadata(OnSourceChanged));

        private static void OnSourceChanged(DependencyObject obj, DependencyPropertyChangedEventArgs e)
        {
            Page page = (Page)obj;

            page.Loaded += PageLoaded;
        }

        private static void PageLoaded(object sender, RoutedEventArgs e)
        {
            Page page = (Page)sender;

            INavigable navSource = GetSource(page);

            if (navSource != null)
            {
                navSource.NavigationService = new NavigationService(page.NavigationService);
            }
        }
    }
}
