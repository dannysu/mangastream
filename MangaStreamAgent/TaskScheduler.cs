using System;
using System.Net;
using System.Linq;
using System.Threading;
using System.Collections.Generic;
using System.IO.IsolatedStorage;
using Microsoft.Phone.BackgroundTransfer;
using Microsoft.Phone.Scheduler;
using Microsoft.Phone.Shell;
using Newtonsoft.Json;
using MangaStreamCommon;

namespace MangaStreamAgent
{
    public class ScheduledAgent : ScheduledTaskAgent
    {
        private MangaDataContext _mangaDB;
        private BackgroundTransfer _backgroundTransfer;
        private AutoResetEvent _waitHandle;

        public ScheduledAgent()
        {
            _mangaDB = new MangaDataContext(Constants._DBConnectionString);
            _backgroundTransfer = new BackgroundTransfer();
            _waitHandle = new AutoResetEvent(false);
        }

        /// <summary>
        /// Agent that runs a scheduled task
        /// </summary>
        /// <param name="task">
        /// The invoked task
        /// </param>
        /// <remarks>
        /// This method is called when a periodic or resource intensive task is invoked
        /// </remarks>
        protected override void OnInvoke(ScheduledTask task)
        {
            if (!_mangaDB.DatabaseExists())
            {
                // The database is expected to be there, if it's not then simply abort scheduled task and any future execution.
                Abort();
                return;
            }

            // Download latest chapters
            WebClient client = new WebClient();
            client.DownloadStringCompleted += new DownloadStringCompletedEventHandler(OnLatestChaptersTransferCompleted);
            client.DownloadStringAsync(new Uri(ServerConstants._serverUri + string.Format(Constants._latestRequestTemplate, ServerConstants._apiKey)));

            _waitHandle.WaitOne(TimeSpan.FromMinutes(9));

            NotifyComplete();
        }

        void OnLatestChaptersTransferCompleted(object sender, DownloadStringCompletedEventArgs e)
        {
            if (null != e.Error)
            {
                _waitHandle.Set();
                return;
            }

            try
            {
                List<MangaAbstractModel> latestChapters = JsonConvert.DeserializeObject<List<MangaAbstractModel>>(e.Result);
                foreach (MangaAbstractModel chapter in latestChapters)
                {
                    chapter.IsRecentChapter = true;
                }

                // TODO: Is there a more efficient way to empty the entire table?
                //       LINQ on Windows Phone doesn't seem to have ExecuteCommand().
                var query = from MangaAbstractModel chapter in _mangaDB.Chapters where chapter.IsRecentChapter == true select chapter;
                _mangaDB.Chapters.DeleteAllOnSubmit(query);
                _mangaDB.SubmitChanges();

                _mangaDB.Chapters.InsertAllOnSubmit(latestChapters);
                _mangaDB.SubmitChanges();

                if (latestChapters.Count > 0)
                {
                    string oldMangaId = string.Empty;
                    if (IsolatedStorageSettings.ApplicationSettings.Contains(Constants._latestMangaId))
                    {
                        oldMangaId = (string)IsolatedStorageSettings.ApplicationSettings[Constants._latestMangaId];
                    }

                    if (!oldMangaId.Equals(latestChapters[0].MangaId))
                    {
                        int releaseCount = 0;

                        // Figure out how many new releases there are
                        foreach (MangaAbstractModel model in latestChapters)
                        {
                            if (model.MangaId.Equals(oldMangaId))
                            {
                                break;
                            }
                            else
                            {
                                releaseCount++;
                            }
                        }

                        // Launch a toast to show that the agent is running.
                        // The toast will not be shown if the foreground application is running.
                        ShellToast toast = new ShellToast();
                        toast.Title = releaseCount > 1 ? "New manga releases" : "New manga release";
                        toast.Content = "";
                        toast.Show();

                        ShellTile appTile = ShellTile.ActiveTiles.First();
                        if (appTile != null)
                        {
                            StandardTileData tileData = new StandardTileData();
                            tileData.BackTitle = "MangaStream";
                            tileData.BackContent = releaseCount > 1 ? "New manga releases" : "New manga release";
                            tileData.Count = releaseCount;

                            appTile.Update(tileData);
                        }
                    }

                    IsolatedStorageSettings.ApplicationSettings[Constants._latestMangaId] = latestChapters[0].MangaId;
                    IsolatedStorageSettings.ApplicationSettings.Save();
                }
            }
            catch (Exception)
            {
            }
            finally
            {
                _waitHandle.Set();
            }
        }
    }
}
