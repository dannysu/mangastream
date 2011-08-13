using Microsoft.Phone.Scheduler;
using MangaStreamCommon;

namespace MangaStreamAgent
{
    public class ScheduledAgent : ScheduledTaskAgent
    {
        private MangaDataContext _mangaDB;
        private BackgroundTransfer _backgroundTransfer;

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
            // Download latest chapters

            // Download manga series

            NotifyComplete();
        }
    }
}
