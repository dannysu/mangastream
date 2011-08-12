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

            ((MainPageViewModel)DataContext).PropertyChanged += new System.ComponentModel.PropertyChangedEventHandler(OnPropertyChanged);
        }

        void OnPropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName.Equals("DoneLoading"))
            {
                HideLoadingOverlay();
            }
        }

        // Load data for the ViewModel Items
        private void MainPage_Loaded(object sender, RoutedEventArgs e)
        {
            ShowLoadingOverlay();
            ((MainPageViewModel)DataContext).OnLoaded();
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

        private void RefreshButton_Click(object sender, EventArgs e)
        {
            ShowLoadingOverlay();
            ((MainPageViewModel)DataContext).OnRefresh();
        }

        private void PhoneApplicationPage_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            LoadingOverlay.Width = e.NewSize.Width;
            LoadingOverlay.Height = e.NewSize.Height;
            ProgressIndicator.Width = e.NewSize.Width;

            // TODO: Are there ways to know exactly what the margins and header sizes are?
            webBrowser1.Width = e.NewSize.Width - 24;
            webBrowser1.Height = e.NewSize.Height - 150;
        }

        private void ClearCacheMenu_Click(object sender, EventArgs e)
        {
            ((MainPageViewModel)DataContext).ClearImagesInCache();
        }

        private void ShowLoadingOverlay()
        {
            LoadingOverlay.Visibility = Visibility.Visible;
            ProgressIndicator.IsIndeterminate = true;
        }

        private void HideLoadingOverlay()
        {
            LoadingOverlay.Visibility = Visibility.Collapsed;
            ProgressIndicator.IsIndeterminate = false;
        }

        private void Pivot_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (Pivot.SelectedIndex == 2)
            {
                webBrowser1.Source = new Uri("http://mobile.twitter.com/mangastream");
            }
        }
    }
}