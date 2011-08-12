using System;
using System.Windows; // For MessageBox
using System.Windows.Controls; // For ListBox and other controls
using System.Collections.ObjectModel;
using System.IO;
using System.Net;
using System.Windows.Threading;
using Newtonsoft.Json;
using System.IO.IsolatedStorage;

namespace MangaStream
{
    public class ChaptersPageViewModel : ViewModelBase
    {
        public ObservableCollection<MangaAbstractModel> ChaptersInSeries { get; private set; }

        public string PivotHeader { get; private set; }
        public string NavigateTarget { get; private set; }

        public ChaptersPageViewModel()
        {
            SetLoadingStatus(false);
        }

        public void OnLoaded()
        {
            PivotHeader = App.AppData.CurrentSeries.SeriesName;
            NotifyPropertyChanged("PivotHeader");

            SetLoadingStatus(true);

            App.AppData.Events = new AppDataEvents();
            App.AppData.Events.DataLoaded += new AppDataEvents.DataLoadedEventHandler(OnDataLoaded);

            App.AppData.StopViewingPage();
            App.AppData.StopViewingChapter();

            if (!App.AppData.IsChaptersInSeriesLoaded)
            {
                App.AppData.LoadChaptersInSeriesAsync(false);
            }
            else
            {
                SetLoadingStatus(false);
            }

            App.AppData.ClearImagesFromExpiredChapters();

            // Even if the data is expired, but it's there, display it anyway
            // TODO: This might be wrong and might cause wrong data from a different series to be displayed
            if (App.AppData.ChaptersInSeries.Count > 0)
            {
                ChaptersInSeries = App.AppData.ChaptersInSeries;
                NotifyPropertyChanged("ChaptersInSeries");
            }
        }

        public void OnSelectChapter(MangaAbstractModel viewModel)
        {
            if (App.AppData.CurrentSeries != null &&
                viewModel.SeriesId.Equals(App.AppData.CurrentSeries.SeriesId))
            {
                App.AppData.ViewChapter(viewModel);
            }
            else
            {
                MessageBox.Show("Unable to find " + viewModel.SeriesName);
            }
        }

        public void OnRefresh()
        {
            SetLoadingStatus(true);

            // force refresh data even if there is already data in the cache
            App.AppData.LoadChaptersInSeriesAsync(true);
        }

        public void ListSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ListBox list = (ListBox)sender;
            if (e.AddedItems.Count == 1 && list.SelectedItem != null)
            {
                OnSelectChapter((MangaAbstractModel)list.SelectedItem);
                list.SelectedItem = null;

                NavigateTarget = "/ViewMangaPage.xaml";
                NotifyPropertyChanged("NavigateTarget");
            }
        }

        void OnDataLoaded(object sender, bool success)
        {
            if (success)
            {
                ChaptersInSeries = App.AppData.ChaptersInSeries;
                NotifyPropertyChanged("ChaptersInSeries");
                App.AppData.ClearImagesFromExpiredChapters();
            }
            else
            {
                MessageBox.Show("Failed to load chapters");
            }
            SetLoadingStatus(false);
        }
    }
}