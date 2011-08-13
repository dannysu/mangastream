using System;
using System.ComponentModel;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using System.Collections.ObjectModel;
using System.IO;
using System.Net;
using System.Windows.Threading;
using Newtonsoft.Json;
using System.IO.IsolatedStorage;

namespace MangaStream
{
    public class MainPageViewModel : ViewModelBase
    {
        private bool _multipleRefreshesInProgress;
        private const string _twitterSource = "http://mobile.twitter.com/mangastream";

        public SeriesByName Series { get; private set; }
        public ObservableCollection<MangaAbstractModel> LatestChapters { get; private set; }

        public ICommand RefreshCommand { get; set; }
        public ICommand ClearCacheCommand { get; set; }
        public ICommand SeriesTapCommand { get; set; }
        public ICommand LatestChapterTapCommand { get; set; }

        public string TwitterSource
        {
            get
            {
                return _twitterSource;
            }
        }

        public MainPageViewModel()
        {
            RefreshCommand = new DelegateCommand(Refresh, CanExecute);
            ClearCacheCommand = new DelegateCommand(ClearCache, CanExecute);
            SeriesTapCommand = new DelegateCommand(SeriesTap, CanExecute);
            LatestChapterTapCommand = new DelegateCommand(LatestChapterTap, CanExecute);

            _multipleRefreshesInProgress = false;
        }

        public void OnLoaded()
        {
            SetLoadingStatus(true);

            App.AppData.Events = new AppDataEvents();
            App.AppData.Events.DataLoaded += new AppDataEvents.DataLoadedEventHandler(OnDataLoaded);

            App.AppData.StopViewingPage();
            App.AppData.StopViewingChapter();
            App.AppData.StopViewingSeries();

            // Check if series data is loaded or if it's not fresh this flag will also be false
            if (!App.AppData.IsSeriesLoaded)
            {
                App.AppData.LoadSeriesAsync(false);
            }

            // If there is deserialized data then display it even if it might not be fresh
            if (App.AppData.Series.Count > 0)
            {
                Series = App.AppData.Series;
                NotifyPropertyChanged("Series");
            }

            // Check if latest releases are loaded or if it's not fresh this flag will also be false
            if (!App.AppData.IsLatestChaptersLoaded)
            {
                App.AppData.LoadLatestChaptersAsync(false);
            }

            // If there is deserialized data then display it even if it might not be fresh
            if (App.AppData.LatestChapters.Count > 0)
            {
                LatestChapters = App.AppData.LatestChapters;
                NotifyPropertyChanged("LatestChapters");
            }

            if (App.AppData.IsSeriesLoaded && App.AppData.IsLatestChaptersLoaded)
            {
                SetLoadingStatus(false);
            }

            if (!App.AppData.IsSeriesLoaded && !App.AppData.IsLatestChaptersLoaded)
            {
                _multipleRefreshesInProgress = true;
            }
        }

        public void OnSelectChapter(MangaAbstractModel viewModel)
        {
            bool found = false;
            foreach (SeriesInGroup group in Series)
            {
                foreach (SeriesModel series in group)
                {
                    if (series.SeriesId.Equals(viewModel.SeriesId))
                    {
                        App.AppData.ViewSeries(series);
                        found = true;
                    }
                }
            }
            if (found)
            {
                App.AppData.ViewChapter(viewModel);
            }
            else
            {
                MessageBox.Show("Unable to find " + viewModel.SeriesName);
            }
        }

        public void OnSelectSeries(SeriesModel viewModel)
        {
            App.AppData.ViewSeries(viewModel);
        }

        public void Refresh(object param)
        {
            SetLoadingStatus(true);

            // force refresh data even if there is already data in the cache
            App.AppData.LoadSeriesAsync(true);
            App.AppData.LoadLatestChaptersAsync(true);

            NotifyPropertyChanged("TwitterSource");

            _multipleRefreshesInProgress = true;
        }

        public void ClearCache(object param)
        {
            App.AppData.ClearImagesInCache();
            MessageBox.Show("Cleared cached images");
        }

        public void SeriesTap(object param)
        {
            if (param == null)
            {
                return;
            }

            SeriesModel model = (SeriesModel)param;

            OnSelectSeries(model);

            NavigationService.Navigate("/ChaptersPage.xaml");
        }

        public void LatestChapterTap(object param)
        {
            if (param == null)
            {
                return;
            }

            MangaAbstractModel model = (MangaAbstractModel)param;
            
            OnSelectChapter(model);

            NavigationService.Navigate("/ViewMangaPage.xaml");
        }

        public bool CanExecute(object param)
        {
            return true;
        }

        void OnDataLoaded(object sender, bool success)
        {
            if (success)
            {
                Series = App.AppData.Series;
                NotifyPropertyChanged("Series");

                LatestChapters = App.AppData.LatestChapters;
                NotifyPropertyChanged("LatestChapters");

                if (App.AppData.IsSeriesLoaded && App.AppData.IsLatestChaptersLoaded)
                {
                    SetLoadingStatus(false);
                }
            }
            else if (!_multipleRefreshesInProgress)
            {
                MessageBox.Show("Failed to load series and latest releases");
                SetLoadingStatus(false);
            }
            _multipleRefreshesInProgress = false;
        }
    }
}