using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using Microsoft.Phone.Controls;

namespace MangaStream
{
    public partial class MainPage : PhoneApplicationPage
    {
        // Constructor
        public MainPage()
        {
            InitializeComponent();
        }

        private void FirstListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (e.AddedItems.Count == 1 && FirstListBox.SelectedItem != null)
            {
                ((MainPageViewModel)DataContext).OnSelectSeries((SeriesModel)FirstListBox.SelectedItem);
                FirstListBox.SelectedItem = null;
                NavigationService.Navigate(new Uri("/ChaptersPage.xaml", UriKind.Relative));
            }
        }

        private void SecondListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (e.AddedItems.Count == 1 && SecondListBox.SelectedItem != null)
            {
                ((MainPageViewModel)DataContext).OnSelectChapter((MangaAbstractModel)SecondListBox.SelectedItem);
                SecondListBox.SelectedItem = null;
                NavigationService.Navigate(new Uri("/ViewMangaPage.xaml", UriKind.Relative));
            }
        }

        private void PhoneApplicationPage_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            // TODO: Are there ways to know exactly what the margins and header sizes are?
            webBrowser1.Width = e.NewSize.Width - 24;
            webBrowser1.Height = e.NewSize.Height - 150;
        }

        private void Pivot_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            /*
            if (Pivot.SelectedIndex == 2)
            {
                webBrowser1.Source = new Uri("http://mobile.twitter.com/mangastream");
            }
            */
        }
    }
}