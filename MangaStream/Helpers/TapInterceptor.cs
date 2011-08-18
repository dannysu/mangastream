using System;
using System.Windows;
using System.Windows.Input;
using System.Windows.Controls;
using Microsoft.Phone.Controls;

namespace MangaStream
{
    public static class TapInterceptor
    {
        public static ISelectable GetSource(DependencyObject obj)
        {
            return (ISelectable)obj.GetValue(SourceProperty);
        }

        public static void SetSource(DependencyObject obj, ISelectable value)
        {
            obj.SetValue(SourceProperty, value);
        }

        public static readonly DependencyProperty SourceProperty =
            DependencyProperty.RegisterAttached("Source", typeof(ISelectable), typeof(Navigator), new PropertyMetadata(OnSourceChanged));

        private static void OnSourceChanged(DependencyObject obj, DependencyPropertyChangedEventArgs e)
        {
            LongListSelector list = (LongListSelector)obj;

            list.Tap += new EventHandler<System.Windows.Input.GestureEventArgs>(OnListTap);
        }

        private static void OnListTap(object sender, System.Windows.Input.GestureEventArgs e)
        {
            LongListSelector list = (LongListSelector)sender;

            ISelectable source = GetSource(list);

            source.SelectedItem = list.SelectedItem;
        }
    }
}
