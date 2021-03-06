﻿using System;
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
using System.Collections.ObjectModel;
using System.IO;
using System.Net;
using System.Windows.Threading;
using Newtonsoft.Json;
using System.IO.IsolatedStorage;
using Microsoft.Phone.BackgroundTransfer;
using System.Linq;
using System.Data.Linq;
using System.Data.Linq.Mapping;
using MangaStreamCommon;

namespace MangaStream
{
    public class AppData : INotifyPropertyChanged
    {
        private const string _savedCurrentlyViewingSeries = "CurrentSeries";
        private const string _savedCurrentlyViewingChapter = "CurrentChapter";
        private const string _savedCurrentlyViewingPage = "CurrentPage";

        // Retention periods
        private const int _oneWeekRetention = 7;
        private const int _oneDayRetention = 1;

        public SeriesByName Series { get; private set; }
        public ObservableCollection<MangaAbstractModel> LatestChapters { get; private set; }
        public ObservableCollection<MangaAbstractModel> ChaptersInSeries { get; private set; }
        public MangaModel Manga { get; private set; }

        public AppDataEvents Events { get; set; }

        private SeriesModel _currentlyViewingSeries;
        private MangaAbstractModel _currentlyViewingChapter;
        private int _currentlyViewingPage;

        private uint _downloadAllContext;
        private Dictionary<uint, uint> _downloadAllProgress;

        private uint _downloadPageContext;
        private Dictionary<uint, uint> _downloadPageProgress;

        private BackgroundTransfer _backgroundTransfer;

        // LINQ to SQL data context for the local database
        private MangaDataContext _mangaDB;

        public AppData()
        {
            this.LatestChapters = new ObservableCollection<MangaAbstractModel>();
            this.Series = new SeriesByName(new List<SeriesModel>());
            this.ChaptersInSeries = new ObservableCollection<MangaAbstractModel>();
            this._currentlyViewingSeries = null;
            this._currentlyViewingChapter = null;
            this._currentlyViewingPage = -1;

            this._downloadAllContext = 0;
            this._downloadAllProgress = new Dictionary<uint, uint>();

            this._downloadPageContext = 0;
            this._downloadPageProgress = new Dictionary<uint, uint>();

            _backgroundTransfer = new BackgroundTransfer();

            _mangaDB = new MangaDataContext(Constants._DBConnectionString);
            if (!_mangaDB.DatabaseExists())
            {
                _mangaDB.CreateDatabase();
            }
        }

        public void ViewSeries(SeriesModel viewModel)
        {
            var query = from MangaAbstractModel chapter in _mangaDB.Chapters where chapter.SeriesId == viewModel.SeriesId && chapter.IsRecentChapter == false select chapter;
            List<MangaAbstractModel> chapters = new List<MangaAbstractModel>(query);

            if (chapters.Count > 0)
            {
                UpdateChaptersInSeries(chapters);

                IsChaptersInSeriesLoaded = (ChaptersInSeries.Count > 0) && IsCreationTimeFresh(chapters[0].CreationTime, _oneDayRetention);
            }
            else
            {
                ChaptersInSeries.Clear();
                IsChaptersInSeriesLoaded = false;
            }
            _currentlyViewingSeries = viewModel;
        }

        public void StopViewingSeries()
        {
            _currentlyViewingSeries = null;
        }

        public SeriesModel CurrentSeries
        {
            get
            {
                return _currentlyViewingSeries;
            }
        }

        public void ViewChapter(MangaAbstractModel viewModel)
        {
            var query = from MangaModel manga in _mangaDB.Manga where manga.MangaId == viewModel.MangaId select manga;
            List<MangaModel> mangaModels = new List<MangaModel>(query);

            if (mangaModels.Count > 0)
            {
                UpdateManga(mangaModels);

                IsChapterLoaded = (Manga != null) && IsCreationTimeFresh(mangaModels[0].CreationTime, _oneWeekRetention);
            }
            else
            {
                Manga = null;
                IsChapterLoaded = false;
            }
            _currentlyViewingChapter = viewModel;
        }

        public void StopViewingChapter()
        {
            _currentlyViewingChapter = null;
        }

        public MangaAbstractModel CurrentChapter
        {
            get
            {
                return _currentlyViewingChapter;
            }
        }

        public void StopViewingPage()
        {
            _currentlyViewingPage = -1;
        }

        public int CurrentPage
        {
            get
            {
                return _currentlyViewingPage;
            }
        }

        public bool IsSeriesLoaded { get; private set; }
        public bool IsLatestChaptersLoaded { get; private set; }
        public bool IsChaptersInSeriesLoaded { get; private set; }
        public bool IsChapterLoaded { get; private set; }

        public void Serialize()
        {
            if (_currentlyViewingSeries != null)
            {
                IsolatedStorageSettings.ApplicationSettings[_savedCurrentlyViewingSeries] = _currentlyViewingSeries.SeriesId;
            }
            else
            {
                IsolatedStorageSettings.ApplicationSettings.Remove(_savedCurrentlyViewingSeries);
            }

            if (_currentlyViewingChapter != null)
            {
                IsolatedStorageSettings.ApplicationSettings[_savedCurrentlyViewingChapter] = _currentlyViewingChapter.MangaId;
            }
            else
            {
                IsolatedStorageSettings.ApplicationSettings.Remove(_savedCurrentlyViewingChapter);
            }

            if (_currentlyViewingPage >= 0)
            {
                IsolatedStorageSettings.ApplicationSettings[_savedCurrentlyViewingPage] = _currentlyViewingPage.ToString();
            }
            else
            {
                IsolatedStorageSettings.ApplicationSettings.Remove(_savedCurrentlyViewingPage);
            }

            IsolatedStorageSettings.ApplicationSettings.Save();
        }

        public void Deserialize()
        {
            bool seriesDataFresh = false;
            try
            {
                IsolatedStorageFile store = IsolatedStorageFile.GetUserStoreForApplication();
                string[] dataFiles = store.GetFileNames(Constants._dataFilePattern);

                string mangaId = null;
                if (IsolatedStorageSettings.ApplicationSettings.Contains(_savedCurrentlyViewingChapter))
                {
                    mangaId = (string)IsolatedStorageSettings.ApplicationSettings[_savedCurrentlyViewingChapter];
                }

                var latestChaptersInDB = from MangaAbstractModel chapter in _mangaDB.Chapters where chapter.IsRecentChapter == true select chapter;
                List<MangaAbstractModel> latestChapters = new List<MangaAbstractModel>(latestChaptersInDB);
                UpdateLatestChapters(latestChapters);

                var seriesInDB = from SeriesModel seriesModel in _mangaDB.Series select seriesModel;
                List<SeriesModel> series = new List<SeriesModel>(seriesInDB);
                UpdateSeries(series);
                if (series.Count > 0)
                {
                    seriesDataFresh = IsCreationTimeFresh(series[0].CreationTime, _oneWeekRetention);

                    // Also try to check if user was viewing a particular series
                    if (IsolatedStorageSettings.ApplicationSettings.Contains(_savedCurrentlyViewingSeries))
                    {
                        string seriesId = (string)IsolatedStorageSettings.ApplicationSettings[_savedCurrentlyViewingSeries];
                        foreach (SeriesModel viewModel in series)
                        {
                            if (viewModel.SeriesId.Equals(seriesId))
                            {
                                ViewSeries(viewModel);

                                // Also try to check if user was viewing a particular chapter in series
                                if (_currentlyViewingSeries != null && mangaId != null)
                                {
                                    bool foundChapter = false;

                                    if (!foundChapter)
                                    {
                                        foreach (MangaAbstractModel chapterViewModel in LatestChapters)
                                        {
                                            if (chapterViewModel.MangaId.Equals(mangaId))
                                            {
                                                ViewChapter(chapterViewModel);
                                                foundChapter = true;
                                                break;
                                            }
                                        }
                                    }

                                    if (!foundChapter)
                                    {
                                        foreach (MangaAbstractModel chapterViewModel in ChaptersInSeries)
                                        {
                                            if (chapterViewModel.MangaId.Equals(mangaId))
                                            {
                                                ViewChapter(chapterViewModel);
                                                foundChapter = true;
                                                break;
                                            }
                                        }
                                    }

                                    if (foundChapter)
                                    {
                                        if (IsolatedStorageSettings.ApplicationSettings.Contains(_savedCurrentlyViewingPage))
                                        {
                                            string page = (string)IsolatedStorageSettings.ApplicationSettings[_savedCurrentlyViewingPage];
                                            try
                                            {
                                                _currentlyViewingPage = int.Parse(page) - 1;
                                            }
                                            catch
                                            {
                                            }
                                        }
                                    }
                                }
                                break;
                            }
                        }
                    }
                }
            }
            catch
            {
            }

            if (Series.Count > 0 && seriesDataFresh)
            {
                IsSeriesLoaded = true;
            }

            if (LatestChapters.Count > 0 && IsCreationTimeFresh(LatestChapters[0].CreationTime, _oneDayRetention))
            {
                IsLatestChaptersLoaded = true;
            }

            if (ChaptersInSeries.Count > 0)
            {
                IsChaptersInSeriesLoaded = true;
            }

            if (Manga != null)
            {
                IsChapterLoaded = true;
            }
        }

        public void ClearImagesInCache()
        {
            try
            {
                IsolatedStorageFile store = IsolatedStorageFile.GetUserStoreForApplication();
                foreach (string fileName in store.GetFileNames())
                {
                    try
                    {
                        if (fileName.EndsWith(".img") || fileName.EndsWith(Constants._iconFileExt) || fileName.EndsWith(".png"))
                        {
                            store.DeleteFile(fileName);
                        }
                    }
                    catch (Exception)
                    {
                    }
                }
            }
            catch (Exception)
            {
            }
        }

        public void ClearImagesFromExpiredChapters()
        {
            try
            {
                IsolatedStorageFile store = IsolatedStorageFile.GetUserStoreForApplication();

                if (App.AppData.ChaptersInSeries != null && App.AppData.ChaptersInSeries.Count > 0)
                {
                    MangaAbstractModel firstItem = App.AppData.ChaptersInSeries[0];

                    string[] files = store.GetFileNames(firstItem.SeriesId + "_*.img");
                    List<string> validMangaIds = new List<string>(App.AppData.ChaptersInSeries.Count);
                    foreach (MangaAbstractModel viewModel in App.AppData.ChaptersInSeries)
                    {
                        validMangaIds.Add(viewModel.MangaId);
                    }

                    foreach (string fileName in files)
                    {
                        if (!fileName.EndsWith(".img") || !fileName.StartsWith(firstItem.SeriesId))
                        {
                            continue;
                        }

                        int start = fileName.IndexOf("_") + 1;
                        int end = fileName.IndexOf("_", start);
                        string mangaId = fileName.Substring(start, end - start);

                        if (!validMangaIds.Contains(mangaId))
                        {
                            store.DeleteFile(fileName);
                        }
                    }
                }
            }
            catch
            {
            }
        }

        #region Load Series Related Functions
        /// <summary>
        /// Creates and adds a few ItemViewModel objects into the Items collection.
        /// </summary>
        public void LoadSeriesAsync(bool forceRefresh)
        {
            if (forceRefresh)
            {
                IsSeriesLoaded = false;
            }
            if (!IsSeriesLoaded)
            {
                string source = ServerConstants._serverUri + string.Format(Constants._seriesRequestTemplate, ServerConstants._apiKey);
                string destination = Constants._downloadPath + Constants._serializedSeriesFile + Constants._dataFileExt;

                _backgroundTransfer.QueueDownload(source, destination, null, new BackgroundTransfer.OnTransferCompleted(OnSeriesTransferCompleted));
            }
        }

        private void OnSeriesTransferCompleted(BackgroundTransferRequest request)
        {
            if (null != request.TransferError)
            {
                DeleteFile(request.DownloadLocation.OriginalString);
                Deployment.Current.Dispatcher.BeginInvoke(new DataLoadedTrigger(TriggerDataLoaded), false);
                return;
            }

            try
            {
                string data = Storage.ReadFileToString(request.DownloadLocation.OriginalString);

                DeleteFile(request.DownloadLocation.OriginalString);

                List<SeriesModel> series = JsonConvert.DeserializeObject<List<SeriesModel>>(data);

                // TODO: Is there a more efficient way to empty the entire table?
                //       LINQ on Windows Phone doesn't seem to have ExecuteCommand().
                _mangaDB.Series.DeleteAllOnSubmit(_mangaDB.Series);
                _mangaDB.SubmitChanges();

                _mangaDB.Series.InsertAllOnSubmit(series);
                _mangaDB.SubmitChanges();

                Deployment.Current.Dispatcher.BeginInvoke(new UpdateSeriesDelegate(UpdateSeries), series);

                IsSeriesLoaded = true;
                Deployment.Current.Dispatcher.BeginInvoke(new DataLoadedTrigger(TriggerDataLoaded), true);
            }
            catch (Exception)
            {
                Deployment.Current.Dispatcher.BeginInvoke(new DataLoadedTrigger(TriggerDataLoaded), false);
            }
        }

        private delegate void UpdateSeriesDelegate(List<SeriesModel> seriesList);
        private void UpdateSeries(List<SeriesModel> seriesList)
        {
            Series = new SeriesByName(seriesList);
            LoadIconsAsync();
        }
        #endregion

        #region Load Latest Chapters Related Functions
        /// <summary>
        /// Creates and adds a few ItemViewModel objects into the Items collection.
        /// </summary>
        public void LoadLatestChaptersAsync(bool forceRefresh)
        {
            if (forceRefresh)
            {
                IsLatestChaptersLoaded = false;
            }
            if (!IsLatestChaptersLoaded)
            {
                string source = ServerConstants._serverUri + string.Format(Constants._latestRequestTemplate, ServerConstants._apiKey);
                string destination = Constants._downloadPath + Constants._serializedLatestChaptersFile + Constants._dataFileExt;

                _backgroundTransfer.QueueDownload(source, destination, null, new BackgroundTransfer.OnTransferCompleted(OnLatestChaptersTransferCompleted));
            }
        }

        private void OnLatestChaptersTransferCompleted(BackgroundTransferRequest request)
        {
            if (null != request.TransferError)
            {
                DeleteFile(request.DownloadLocation.OriginalString);
                Deployment.Current.Dispatcher.BeginInvoke(new DataLoadedTrigger(TriggerDataLoaded), false);
                return;
            }

            try
            {
                string data = Storage.ReadFileToString(request.DownloadLocation.OriginalString);

                DeleteFile(request.DownloadLocation.OriginalString);

                List<MangaAbstractModel> latestChapters = JsonConvert.DeserializeObject<List<MangaAbstractModel>>(data);
                foreach (MangaAbstractModel chapter in latestChapters)
                {
                    chapter.IsRecentChapter = true;
                }

                if (latestChapters.Count > 0)
                {
                    IsolatedStorageSettings.ApplicationSettings[Constants._latestMangaId] = latestChapters[0].MangaId;
                    IsolatedStorageSettings.ApplicationSettings.Save();
                }

                // TODO: Is there a more efficient way to empty the entire table?
                //       LINQ on Windows Phone doesn't seem to have ExecuteCommand().
                var query = from MangaAbstractModel chapter in _mangaDB.Chapters where chapter.IsRecentChapter == true select chapter;
                _mangaDB.Chapters.DeleteAllOnSubmit(query);
                _mangaDB.SubmitChanges();

                _mangaDB.Chapters.InsertAllOnSubmit(latestChapters);
                _mangaDB.SubmitChanges();

                Deployment.Current.Dispatcher.BeginInvoke(new UpdateLatestChaptersDelegate(UpdateLatestChapters), latestChapters);

                IsLatestChaptersLoaded = true;
                Deployment.Current.Dispatcher.BeginInvoke(new DataLoadedTrigger(TriggerDataLoaded), true);
            }
            catch (Exception)
            {
                Deployment.Current.Dispatcher.BeginInvoke(new DataLoadedTrigger(TriggerDataLoaded), false);
            }
        }

        private delegate void UpdateLatestChaptersDelegate(List<MangaAbstractModel> chaptersList);
        private void UpdateLatestChapters(List<MangaAbstractModel> chaptersList)
        {
            LatestChapters.Clear();

            foreach (MangaAbstractModel viewModel in chaptersList)
            {
                LatestChapters.Add(viewModel);
            }
        }
        #endregion

        #region Load Chapters In Series Related Functions
        /// <summary>
        /// Creates and adds a few ItemViewModel objects into the Items collection.
        /// </summary>
        public void LoadChaptersInSeriesAsync(bool forceRefresh)
        {
            if (forceRefresh)
            {
                IsChaptersInSeriesLoaded = false;
            }
            if (!IsChaptersInSeriesLoaded)
            {
                string source = ServerConstants._serverUri + string.Format(Constants._chaptersRequestTemplate, ServerConstants._apiKey, _currentlyViewingSeries.SeriesId);
                string destination = Constants._downloadPath + Constants._serializedChaptersInSeriesFile + _currentlyViewingSeries.SeriesId + Constants._dataFileExt;
                string tag = _currentlyViewingSeries.SeriesId;

                _backgroundTransfer.QueueDownload(source, destination, tag, new BackgroundTransfer.OnTransferCompleted(OnChaptersInSeriesTransferCompleted));
            }
        }

        private void OnChaptersInSeriesTransferCompleted(BackgroundTransferRequest request)
        {
            if (null != request.TransferError)
            {
                DeleteFile(request.DownloadLocation.OriginalString);
                Deployment.Current.Dispatcher.BeginInvoke(new DataLoadedTrigger(TriggerDataLoaded), false);
                return;
            }

            try
            {
                string data = Storage.ReadFileToString(request.DownloadLocation.OriginalString);

                DeleteFile(request.DownloadLocation.OriginalString);

                List<MangaAbstractModel> chaptersList = JsonConvert.DeserializeObject<List<MangaAbstractModel>>(data);

                // TODO: Is there a more efficient way to empty the entire table?
                //       LINQ on Windows Phone doesn't seem to have ExecuteCommand().
                var query = from MangaAbstractModel chapter in _mangaDB.Chapters where chapter.SeriesId == request.Tag && chapter.IsRecentChapter == false select chapter;
                _mangaDB.Chapters.DeleteAllOnSubmit(query);
                _mangaDB.SubmitChanges();

                _mangaDB.Chapters.InsertAllOnSubmit(chaptersList);
                _mangaDB.SubmitChanges();

                Deployment.Current.Dispatcher.BeginInvoke(new UpdateChaptersDelegate(UpdateChaptersInSeries), chaptersList);

                IsChaptersInSeriesLoaded = true;
                Deployment.Current.Dispatcher.BeginInvoke(new DataLoadedTrigger(TriggerDataLoaded), true);

                ClearImagesFromExpiredChapters();
            }
            catch (Exception)
            {
                Deployment.Current.Dispatcher.BeginInvoke(new DataLoadedTrigger(TriggerDataLoaded), false);
            }
        }

        private delegate void UpdateChaptersDelegate(List<MangaAbstractModel> chaptersList);
        private void UpdateChaptersInSeries(List<MangaAbstractModel> chaptersList)
        {
            ChaptersInSeries.Clear();

            foreach (MangaAbstractModel viewModel in chaptersList)
            {
                ChaptersInSeries.Add(viewModel);
            }
        }
        #endregion

        #region Load Chapter Related Functions
        /// <summary>
        /// Creates and adds a few ItemViewModel objects into the Items collection.
        /// </summary>
        public void LoadChapterAsync(bool forceRefresh)
        {
            if (forceRefresh)
            {
                IsChapterLoaded = false;
            }
            if (!IsChapterLoaded)
            {
                string source = ServerConstants._serverUri + string.Format(Constants._chapterRequestTemplate, ServerConstants._apiKey, _currentlyViewingChapter.MangaId);
                string destination = Constants._downloadPath + Constants._serializedMangaFile + _currentlyViewingChapter.MangaId + Constants._dataFileExt;
                string tag = _currentlyViewingChapter.MangaId;

                _backgroundTransfer.QueueDownload(source, destination, tag, new BackgroundTransfer.OnTransferCompleted(OnChapterTransferCompleted));
            }
        }

        private void OnChapterTransferCompleted(BackgroundTransferRequest request)
        {
            if (null != request.TransferError)
            {
                DeleteFile(request.DownloadLocation.OriginalString);
                Deployment.Current.Dispatcher.BeginInvoke(new DataLoadedTrigger(TriggerDataLoaded), false);
                return;
            }

            try
            {
                string data = Storage.ReadFileToString(request.DownloadLocation.OriginalString);

                DeleteFile(request.DownloadLocation.OriginalString);

                List<MangaModel> mangaList = JsonConvert.DeserializeObject<List<MangaModel>>(data);
                foreach (MangaModel mangaModel in mangaList)
                {
                    foreach (PageModel pageModel in mangaModel.Pages)
                    {
                        pageModel.MangaId = mangaModel.MangaId;
                        foreach (ImageModel imageModel in pageModel.Images)
                        {
                            imageModel.MangaId = mangaModel.MangaId;
                            imageModel.Page = pageModel.Page;
                        }
                    }
                }

                var imageQuery = from ImageModel imageModel in _mangaDB.Images where imageModel.MangaId == request.Tag select imageModel;
                var pageQuery = from PageModel pageModel in _mangaDB.Pages where pageModel.MangaId == request.Tag select pageModel;
                var mangaQuery = from MangaModel mangaModel in _mangaDB.Manga where mangaModel.MangaId == request.Tag select mangaModel;

                _mangaDB.Images.DeleteAllOnSubmit(imageQuery);
                _mangaDB.SubmitChanges();

                _mangaDB.Pages.DeleteAllOnSubmit(pageQuery);
                _mangaDB.SubmitChanges();

                _mangaDB.Manga.DeleteAllOnSubmit(mangaQuery);
                _mangaDB.SubmitChanges();

                _mangaDB.Manga.InsertAllOnSubmit(mangaList);
                foreach (MangaModel mangaModel in mangaList)
                {
                    _mangaDB.Pages.InsertAllOnSubmit(mangaModel.Pages);
                    foreach (PageModel pageModel in mangaModel.Pages)
                    {
                        _mangaDB.Images.InsertAllOnSubmit(pageModel.Images);
                    }
                }
                _mangaDB.SubmitChanges();

                Deployment.Current.Dispatcher.BeginInvoke(new UpdateMangaDelegate(UpdateManga), mangaList);

                IsChapterLoaded = true;
                Deployment.Current.Dispatcher.BeginInvoke(new DataLoadedTrigger(TriggerDataLoaded), true);
            }
            catch (Exception)
            {
                Deployment.Current.Dispatcher.BeginInvoke(new DataLoadedTrigger(TriggerDataLoaded), false);
            }
        }
        #endregion

        public delegate void GetPageAsyncCompletedTrigger(PageModel pageModel);
        private void TriggerGetPageAsyncCompleted(PageModel pageModel)
        {
            if (Events != null)
            {
                Events.InvokeGetPageAsyncCompleted(this, pageModel);
            }
        }

        public delegate void GetAllPagesAsyncCompletedTrigger(bool success);
        private void TriggerGetAllPagesAsyncCompleted(bool success)
        {
            if (Events != null)
            {
                Events.InvokeGetAllPagesAsyncCompleted(this, success);
            }
        }

        public delegate void GetIconAsyncCompletedTrigger(string seriesId);
        private void TriggerLoadIconAsyncCompleted(string seriesId)
        {
            if (Series != null)
            {
                foreach (SeriesInGroup group in Series)
                {
                    foreach (SeriesModel viewModel in group)
                    {
                        if (viewModel.SeriesId.Equals(seriesId))
                        {
                            IsolatedStorageFile store = IsolatedStorageFile.GetUserStoreForApplication();
                            IsolatedStorageFileStream stream = null;
                            try
                            {
                                stream = store.OpenFile(seriesId + Constants._iconFileExt, FileMode.Open);

                                BitmapImage image = new BitmapImage();
                                image.SetSource(stream);

                                viewModel.IconImage = image;

                                stream.Close();
                                stream = null;
                            }
                            catch
                            {
                            }
                            finally
                            {
                                if (stream != null)
                                {
                                    stream.Close();
                                }
                            }
                            return;
                        }
                    }
                }
            }
        }

        public void LoadPageAsync(PageModel page, List<string> cachePaths, bool forceRefresh)
        {
            _currentlyViewingPage = page.Page;
            try
            {
                _downloadPageContext++;

                uint numDownloadPending = 0;
                IsolatedStorageFile store = IsolatedStorageFile.GetUserStoreForApplication();
                for (int i = 0; i < cachePaths.Count; i++)
                {
                    bool found = false;

                    string cachePath = cachePaths[i];
                    found = store.FileExists(cachePath);

                    if (forceRefresh || !found)
                    {
                        if (forceRefresh)
                        {
                            store.DeleteFile(cachePath);
                        }

                        WebClient webClient = new WebClient();
                        webClient.OpenReadCompleted += new OpenReadCompletedEventHandler(OnDownloadImageCompletedForDownloadPage);
                        webClient.OpenReadAsync(new Uri(page.Images[i].Url), new DownloadImageState(cachePath, page, _downloadPageContext));

                        numDownloadPending++;
                    }
                }

                if (numDownloadPending > 0)
                {
                    _downloadPageProgress.Add(_downloadPageContext, numDownloadPending);
                }
                else
                {
                    TriggerGetPageAsyncCompleted(page);
                }
            }
            catch
            {
                PageModel pageModel = new PageModel();
                pageModel.Images = new EntitySet<ImageModel>();
                TriggerGetPageAsyncCompleted(pageModel);
            }
        }

        void OnDownloadImageCompletedForDownloadPage(object sender, OpenReadCompletedEventArgs e)
        {
            DownloadImageState state = (DownloadImageState)e.UserState;

            if (!_downloadPageProgress.ContainsKey(state.Context))
            {
                return;
            }

            _downloadPageProgress[state.Context]--;

            if (e.Error == null && e.Result != null)
            {
                try
                {
                    Storage.SaveStreamToFile(e.Result, (string)state.CachePath);

                    if (_downloadPageProgress[state.Context] == 0)
                    {
                        Deployment.Current.Dispatcher.BeginInvoke(new GetPageAsyncCompletedTrigger(TriggerGetPageAsyncCompleted), state.Page);
                    }
                }
                catch
                {
                    _downloadPageProgress.Remove(state.Context);
                    PageModel pageModel = new PageModel();
                    pageModel.Images = new EntitySet<ImageModel>();
                    Deployment.Current.Dispatcher.BeginInvoke(new GetPageAsyncCompletedTrigger(TriggerGetPageAsyncCompleted), pageModel);
                }
            }
            else
            {
                _downloadPageProgress.Remove(state.Context);
                PageModel pageModel = new PageModel();
                pageModel.Images = new EntitySet<ImageModel>();
                Deployment.Current.Dispatcher.BeginInvoke(new GetPageAsyncCompletedTrigger(TriggerGetPageAsyncCompleted), pageModel);
            }
        }

        public void LoadAllPagesAsync(EntitySet<PageModel> pages, List<List<string>> cachePaths)
        {
            try
            {
                _downloadAllContext++;

                List<KeyValuePair<string, string>> pendingDownloadList = new List<KeyValuePair<string,string>>();

                IsolatedStorageFile store = IsolatedStorageFile.GetUserStoreForApplication();
                for (int i = 0; i < cachePaths.Count; i++)
                {
                    List<string> imageCachePaths = cachePaths[i];

                    for (int j = 0; j < imageCachePaths.Count; j++)
                    {
                        string cachePath = imageCachePaths[j];
                        if (!store.FileExists(cachePath))
                        {
                            PageModel pageModel = pages[i];
                            ImageModel imageModel = pageModel.Images[j];

                            pendingDownloadList.Add(new KeyValuePair<string, string>(imageModel.Url, Constants._downloadPath + cachePath));           
                        }
                    }
                }

                if (pendingDownloadList.Count > 0)
                {
                    _downloadAllProgress.Add(_downloadAllContext, (uint)pendingDownloadList.Count);
                    foreach (KeyValuePair<string, string> pair in pendingDownloadList)
                    {
                        _backgroundTransfer.QueueDownload(pair.Key, pair.Value, _downloadAllContext.ToString(), new BackgroundTransfer.OnTransferCompleted(OnDownloadPageCompletedForDownloadAllPages));
                    }
                }
                else
                {
                    TriggerGetAllPagesAsyncCompleted(true);
                }
            }
            catch
            {
                TriggerGetAllPagesAsyncCompleted(false);
            }
        }

        void OnDownloadPageCompletedForDownloadAllPages(BackgroundTransferRequest request)
        {
            uint context = uint.Parse(request.Tag);

            if (!_downloadAllProgress.ContainsKey(context))
            {
                return;
            }

            _downloadAllProgress[context]--;

            if (null != request.TransferError)
            {
                Deployment.Current.Dispatcher.BeginInvoke(new GetAllPagesAsyncCompletedTrigger(TriggerGetAllPagesAsyncCompleted), false);
                return;
            }

            try
            {
                IsolatedStorageFile store = IsolatedStorageFile.GetUserStoreForApplication();
                store.MoveFile(request.DownloadLocation.OriginalString, Path.GetFileName(request.DownloadLocation.OriginalString));

                if (_downloadAllProgress[context] == 0)
                {
                    Deployment.Current.Dispatcher.BeginInvoke(new GetAllPagesAsyncCompletedTrigger(TriggerGetAllPagesAsyncCompleted), true);
                }
            }
            catch (Exception)
            {
                _downloadAllProgress.Remove(context);
                Deployment.Current.Dispatcher.BeginInvoke(new GetAllPagesAsyncCompletedTrigger(TriggerGetAllPagesAsyncCompleted), false);
            }
        }

        #region Load Icon Related Functions
        public void LoadIconsAsync()
        {
            try
            {
                IsolatedStorageFile store = IsolatedStorageFile.GetUserStoreForApplication();
                foreach (SeriesInGroup group in Series)
                {
                    foreach (SeriesModel viewModel in group)
                    {
                        string destination = Constants._downloadPath + viewModel.SeriesId + Constants._iconFileExt;
                        if (!store.FileExists(destination))
                        {
                            _backgroundTransfer.QueueDownload(viewModel.Icon, destination, viewModel.SeriesId, new BackgroundTransfer.OnTransferCompleted(OnIconTransferCompleted));
                        }
                        else
                        {
                            TriggerLoadIconAsyncCompleted(viewModel.SeriesId);
                        }
                    }
                }
            }
            catch
            {
                // do nothing, the series would just have no icon shown
            }
        }

        private void OnIconTransferCompleted(BackgroundTransferRequest request)
        {
            if (null != request.TransferError)
            {
                DeleteFile(request.DownloadLocation.OriginalString);
                return;
            }

            try
            {
                DeleteFile(request.Tag + Constants._iconFileExt);
                IsolatedStorageFile.GetUserStoreForApplication().MoveFile(request.DownloadLocation.OriginalString, Path.GetFileName(request.DownloadLocation.OriginalString));

                Deployment.Current.Dispatcher.BeginInvoke(new GetIconAsyncCompletedTrigger(TriggerLoadIconAsyncCompleted), request.Tag);
            }
            catch (Exception)
            {
                // do nothing, the series would just have no icon shown
            }
        }
        #endregion

        public delegate void DataLoadedTrigger(bool success);
        private void TriggerDataLoaded(bool success)
        {
            if (Events != null)
            {
                Events.InvokeDataLoaded(this, success);
            }
        }

        private delegate void UpdateMangaDelegate(List<MangaModel> mangaList);
        private void UpdateManga(List<MangaModel> mangaList)
        {
            if (mangaList.Count > 0)
            {
                Manga = mangaList[0];
            }
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

        #region Helper Functions
        bool IsCreationTimeFresh(DateTime creationTime, int retentionPeriod)
        {
            if (DateTime.Now.Subtract(new TimeSpan(retentionPeriod, 0, 0, 0)).CompareTo(creationTime) <= 0)
            {
                return true;
            }
            return false;
        }

        void DeleteFile(string filePath)
        {
            IsolatedStorageFile store = IsolatedStorageFile.GetUserStoreForApplication();
            try
            {
                store.DeleteFile(filePath);
            }
            catch (Exception)
            {
            }
        }
        #endregion

        private class DownloadAllPagesState
        {
            public string CachePath { get; private set; }
            public List<PageModel> Pages { get; private set; }
            public uint Context { get; private set; }

            public DownloadAllPagesState(string cachePath, List<PageModel> pages, uint context)
            {
                this.CachePath = cachePath;
                this.Pages = pages;
                this.Context = context;
            }
        }

        private class DownloadImageState
        {
            public string CachePath { get; private set; }
            public PageModel Page { get; private set; }
            public uint Context { get; private set; }

            public DownloadImageState(string cachePath, PageModel page, uint context)
            {
                this.CachePath = cachePath;
                this.Page = page;
                this.Context = context;
            }
        }

        private class CacheEntry
        {
            public string Value { get; private set; }
            public bool Updated { get; private set; }
            public bool Expired { get; private set; }

            public CacheEntry(string value, bool updated, bool expired)
            {
                this.Value = value;
                this.Updated = updated;
                this.Expired = expired;
            }
        }
    }
}