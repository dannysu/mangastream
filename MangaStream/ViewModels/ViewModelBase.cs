using System;
using System.ComponentModel;

namespace MangaStream
{
    public class ViewModelBase : INotifyPropertyChanged
    {
        public bool Loading { get; private set; }

        protected void SetLoadingStatus(bool status)
        {
            Loading = status;
            NotifyPropertyChanged("Loading");
        }

        #region INotifyPropertyChanged Members

        public event PropertyChangedEventHandler PropertyChanged;
        protected void NotifyPropertyChanged(String propertyName)
        {
            PropertyChangedEventHandler handler = PropertyChanged;
            if (null != handler)
            {
                handler(this, new PropertyChangedEventArgs(propertyName));
            }
        }

        #endregion
    }
}
