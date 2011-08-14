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
using Microsoft.Phone.Shell;
using Microsoft.Phone.Controls;
using MangaStreamCommon;

namespace MangaStream
{
    public class PageSelectionModel : INotifyPropertyChanged
    {
        public string PageNumber { get; private set; }

        public PageSelectionModel(string pageNumber)
        {
            PageNumber = pageNumber;
        }

        public event PropertyChangedEventHandler PropertyChanged;
        private void NotifyPropertyChanged(String propertyName)
        {
            PropertyChangedEventHandler handler = PropertyChanged;
            if (null != handler)
            {
                handler(this, new PropertyChangedEventArgs(propertyName));
            }
        }
    }

    public class ViewMangaPageModel : ViewModelBase
    {
        private int _currentViewingPage;

        public bool JumpToPageOverlayVisible { get; private set; }
        public bool PreviousAllowed { get; private set; }
        public bool NextAllowed { get; private set; }
        public string DisplayContent { get; private set; }
        public ObservableCollection<PageSelectionModel> Pages { get; private set; }

        public ICommand BackCommand { get; set; }
        public ICommand PreviousCommand { get; set; }
        public ICommand NextCommand { get; set; }
        public ICommand RefreshCommand { get; set; }
        public ICommand JumpToPageCommand { get; set; }
        public ICommand DownloadAllCommand { get; set; }

        public ICommand TapCommand { get; set; }

        public ViewMangaPageModel()
        {
            BackCommand = new DelegateCommand(OnBackKeyPress, CanExecute);
            PreviousCommand = new DelegateCommand(OnPreviousClicked, CanExecute);
            NextCommand = new DelegateCommand(OnNextClicked, CanExecute);
            RefreshCommand = new DelegateCommand(OnRefresh, CanExecute);
            JumpToPageCommand = new DelegateCommand(OnJumpToPage, CanExecute);
            DownloadAllCommand = new DelegateCommand(OnDownloadAll, CanExecute);

            TapCommand = new DelegateCommand(OnListTap, CanExecute);

            SetLoadingStatus(false);

            _currentViewingPage = 0;
            PreviousAllowed = false;
            NextAllowed = false;

            Pages = new ObservableCollection<PageSelectionModel>();

            App.AppData.Events = new AppDataEvents();
            App.AppData.Events.DataLoaded += new AppDataEvents.DataLoadedEventHandler(OnDataLoaded);
            App.AppData.Events.GetPageAsyncCompleted += new AppDataEvents.GetPageAsyncCompletedEventHandler(OnGetPageAsyncCompleted);
            App.AppData.Events.GetAllPagesAsyncCompleted += new AppDataEvents.GetAllPagesAsyncCompletedEventHandler(OnGetAllPagesAsyncCompleted);
        }

        public void OnLoaded()
        {
            SetLoadingStatus(true);

            JumpToPageOverlayVisible = false;
            NotifyPropertyChanged("JumpToPageOverlayVisible");

            _currentViewingPage = 0;
            PreviousAllowed = false;
            NextAllowed = false;

            App.AppData.Events = new AppDataEvents();
            App.AppData.Events.DataLoaded += new AppDataEvents.DataLoadedEventHandler(OnDataLoaded);
            App.AppData.Events.GetPageAsyncCompleted += new AppDataEvents.GetPageAsyncCompletedEventHandler(OnGetPageAsyncCompleted);
            App.AppData.Events.GetAllPagesAsyncCompleted += new AppDataEvents.GetAllPagesAsyncCompletedEventHandler(OnGetAllPagesAsyncCompleted);

            if (!App.AppData.IsChapterLoaded)
            {
                App.AppData.LoadChapterAsync(false);
            }
            else
            {
                _currentViewingPage = App.AppData.CurrentPage;
                if (_currentViewingPage < 0)
                {
                    _currentViewingPage = 0;
                }
                else if (App.AppData.Manga != null)
                {
                    if (_currentViewingPage >= App.AppData.Manga.Pages.Count)
                    {
                        _currentViewingPage = 0;
                    }
                }

                SetLoadingStatus(false);
            }

            LoadPage(_currentViewingPage, false);
        }

        public void OnBackKeyPress(object param)
        {
            if (JumpToPageOverlayVisible)
            {
                HideJumpToPageOverlay(new EventHandler(DoNothingEventHandler));

                System.ComponentModel.CancelEventArgs e = (System.ComponentModel.CancelEventArgs)param;
                if (e != null)
                {
                    e.Cancel = true;
                }
            }
        }

        private void InitializePages()
        {
            if (App.AppData.Manga == null)
            {
                return;
            }

            for (int i = 0; i < App.AppData.Manga.Pages.Count; i++)
            {
                Pages.Add(new PageSelectionModel((i + 1).ToString()));
            }
        }

        public void OnPreviousClicked(object param)
        {
            if (!Loading)
            {
                HideJumpToPageOverlay(new EventHandler(DoPrevious));
            }
        }

        private void DoPrevious(object sender, EventArgs e)
        {
            if (App.AppData.Manga == null)
            {
                return;
            }

            SetLoadingStatus(true);
            MangaModel manga = App.AppData.Manga;
            if ((_currentViewingPage - 1) >= 0)
            {
                LoadPage(_currentViewingPage - 1, false);
            }
        }

        public void OnNextClicked(object param)
        {
            if (!Loading)
            {
                HideJumpToPageOverlay(new EventHandler(DoNext));
            }
        }

        private void DoNext(object sender, EventArgs e)
        {
            if (App.AppData.Manga == null)
            {
                return;
            }

            SetLoadingStatus(true);
            MangaModel manga = App.AppData.Manga;
            if (_currentViewingPage + 1 < manga.Pages.Count)
            {
                LoadPage(_currentViewingPage + 1, false);
            }
        }

        public void JumpToPage(int i)
        {
            if (App.AppData.Manga == null || Loading)
            {
                return;
            }

            SetLoadingStatus(true);
            MangaModel manga = App.AppData.Manga;
            if (i >= 0 && i < manga.Pages.Count)
            {
                LoadPage(i, false);
            }
        }

        public void OnRefresh(object param)
        {
            if (!Loading)
            {
                HideJumpToPageOverlay(new EventHandler(DoRefresh));
            }
        }

        private void DoRefresh(object sender, EventArgs e)
        {
            SetLoadingStatus(true);
            App.AppData.LoadChapterAsync(true);
        }

        public void OnJumpToPage(object param)
        {
            ShowJumpToPageOverlay();
        }

        public void OnDownloadAll(object param)
        {
            if (!Loading)
            {
                HideJumpToPageOverlay(new EventHandler(DoDownloadAll));
            }
        }

        private void DoDownloadAll(object sender, EventArgs e)
        {
            if (App.AppData.Manga == null)
            {
                return;
            }

            SetLoadingStatus(true);

            MangaModel manga = App.AppData.Manga;
            List<List<string>> cachePaths = new List<List<string>>(manga.Pages.Count);

            foreach (PageModel page in manga.Pages)
            {
                List<string> tempList = new List<string>();
                cachePaths.Add(tempList);

                int count = 0;
                foreach (ImageModel image in page.Images)
                {
                    tempList.Add(GetCachePath(manga, page, count));
                    count++;
                }
            }

            App.AppData.LoadAllPagesAsync(manga.Pages, cachePaths);

            PhoneApplicationService.Current.UserIdleDetectionMode = IdleDetectionMode.Disabled;
        }

        public void OnListTap(object param)
        {
            if (param == null)
            {
                return;
            }

            int index = (int)param;
            HideJumpToPageOverlayAfterSelection(index);
        }

        private bool CanExecute(object param)
        {
            return true;
        }

        void OnDataLoaded(object sender, bool success)
        {
            if (success)
            {
                InitializePages();
                LoadPage(_currentViewingPage, true);
            }
            else
            {
                if (App.AppData.Manga != null)
                {
                    MessageBox.Show("Failed to load pages for manga. Will use cached data instead.");
                }
                else
                {
                    MessageBox.Show("Failed to load pages for manga");
                }
                NotifyPropertyChanged("DoneLoading");
            }
            SetLoadingStatus(false);
        }

        void LoadPage(int page, bool forceRefresh)
        {
            if (App.AppData.Manga != null)
            {
                MangaModel manga = App.AppData.Manga;
                List<string> cachePaths = new List<string>();
                for (int i = 0; i < manga.Pages[page].Images.Count; i++)
                {
                    string cachePath = GetCachePath(manga, manga.Pages[page], i);
                    cachePaths.Add(cachePath);
                }
                App.AppData.LoadPageAsync(manga.Pages[page], cachePaths, forceRefresh);
                PhoneApplicationService.Current.UserIdleDetectionMode = IdleDetectionMode.Disabled;
            }
        }

        void OnGetPageAsyncCompleted(object sender, PageModel pageModel)
        {
            if (pageModel != null && pageModel.Images.Count >= 1)
            {
                if (App.AppData.Manga != null)
                {
                    MangaModel manga = App.AppData.Manga;

                    string content = string.Empty;

                    if (pageModel.Images.Count == 1)
                    {
                        string cachePath = GetCachePath(manga, pageModel, 0);
                        content = "<html><head><meta name=\"Viewport\" content=\"width=800; height=800; user-scaleable=yes;\"/></head><body><img src=\"" + cachePath + "\"/></body></html>";
                    }
                    else
                    {
                        StringBuilder bodyBuilder = new StringBuilder();
                        StringBuilder styleBuilder = new StringBuilder();
                        for (int i = 0; i < pageModel.Images.Count; i++)
                        {
                            ImageModel imageModel = pageModel.Images[i];
                            styleBuilder.Append("#p" + i + "{position:absolute;z-index:" + imageModel.ZIndex + ";width:" + imageModel.Width + "px;height:" + imageModel.Height + "px;top:" + imageModel.Top + "px;left:" + imageModel.Left + "px}\n");
                            bodyBuilder.Append("<div id=\"p" + i + "\"><img src=\"" + GetCachePath(manga, pageModel, i) + "\"/></div>");
                        }

                        content = "<html><head><meta name=\"Viewport\" content=\"width=800; height=800; user-scaleable=yes;\"/><style type=\"text/css\">" + styleBuilder.ToString() + "</style></head><body><div>" + bodyBuilder.ToString() + "</div></body></html>";
                    }

                    IsolatedStorageFileStream stream = null;
                    try
                    {
                        byte[] buffer = Encoding.UTF8.GetBytes(content);
                        IsolatedStorageFile store = IsolatedStorageFile.GetUserStoreForApplication();
                        stream = store.CreateFile("index.html");
                        stream.Write(buffer, 0, buffer.Length);
                    }
                    catch (Exception)
                    {
                        MessageBox.Show("Couldn't show content");
                    }
                    finally
                    {
                        if (stream != null)
                        {
                            stream.Close();
                        }
                    }

                    _currentViewingPage = pageModel.Page - 1;

                    bool oldNextAllowed = NextAllowed;
                    NextAllowed = (_currentViewingPage < manga.Pages.Count - 1);
                    if (oldNextAllowed != NextAllowed)
                    {
                        NotifyPropertyChanged("NextAllowed");
                    }

                    bool oldPreviousAllowed = PreviousAllowed;
                    PreviousAllowed = (_currentViewingPage > 0);
                    if (oldPreviousAllowed != PreviousAllowed)
                    {
                        NotifyPropertyChanged("PreviousAllowed");
                    }
                }
                else
                {
                    // Couldn't display content
                }
                DisplayContent = "index.html";
                NotifyPropertyChanged("DisplayContent");
            }
            else
            {
                MessageBox.Show("Failed to retrieve images. Please try refresh again.");
            }
            NotifyPropertyChanged("DoneLoading");
            PhoneApplicationService.Current.UserIdleDetectionMode = IdleDetectionMode.Enabled;
            SetLoadingStatus(false);
        }

        void OnGetAllPagesAsyncCompleted(object sender, bool success)
        {
            if (success)
            {
                MessageBox.Show("Downloaded all pages for offline viewing.");
            }
            else
            {
                // FIXME: In failure case, this function can be called multiple times and so many many error message boxes that user needs to dismiss
                MessageBox.Show("An unexpected error occurred when downloading all pages.");
            }
            NotifyPropertyChanged("DoneLoading");
            PhoneApplicationService.Current.UserIdleDetectionMode = IdleDetectionMode.Enabled;
            SetLoadingStatus(false);
        }

        // TODO: Figure out how to hook up show/hide transition in View/XAML
        private void ShowJumpToPageOverlay()
        {
            InitializePages();

            JumpToPageOverlayVisible = true;
            NotifyPropertyChanged("JumpToPageOverlayVisible");
        }

        // TODO: Figure out how to hook up show/hide transition in View/XAML
        private void HideJumpToPageOverlay(EventHandler eventHandler)
        {
            JumpToPageOverlayVisible = false;
            NotifyPropertyChanged("JumpToPageOverlayVisible");
            eventHandler.Invoke(this, new EventArgs());

            Pages.Clear();
        }

        // TODO: Figure out how to hook up show/hide transition in View/XAML
        private void HideJumpToPageOverlayAfterSelection(int index)
        {
            JumpToPageOverlayVisible = false;
            NotifyPropertyChanged("JumpToPageOverlayVisible");
            JumpToPage(index);

            Pages.Clear();
        }

        private void DoNothingEventHandler(object sender, EventArgs e)
        {
        }

        private string GetCachePath(MangaModel mangaModel, PageModel pageModel, int count)
        {
            return mangaModel.SeriesId + "_" + mangaModel.MangaId + "_" + pageModel.Page + "_" + count + ".img";
        }
    }
}