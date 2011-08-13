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
using MangaStreamCommon;

namespace MangaStream
{
    public class AppDataEvents
    {
        public delegate void GetAllPagesAsyncCompletedEventHandler(object sender, bool success);
        public event GetAllPagesAsyncCompletedEventHandler GetAllPagesAsyncCompleted;
        public void InvokeGetAllPagesAsyncCompleted(AppData appData, bool success)
        {
            GetAllPagesAsyncCompleted.Invoke(appData, success);
        }

        public delegate void GetPageAsyncCompletedEventHandler(object sender, PageModel pageModel);
        public event GetPageAsyncCompletedEventHandler GetPageAsyncCompleted;
        public void InvokeGetPageAsyncCompleted(AppData appData, PageModel pageModel)
        {
            GetPageAsyncCompleted.Invoke(appData, pageModel);
        }

        public delegate void DataLoadedEventHandler(object sender, bool success);
        public event DataLoadedEventHandler DataLoaded;
        public  void InvokeDataLoaded(AppData appData, bool success)
        {
            DataLoaded.Invoke(appData, success);
        }
    }
}