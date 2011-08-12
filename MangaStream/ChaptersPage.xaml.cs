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
    public partial class ChaptersPage : PhoneApplicationPage
    {
        public ChaptersPage()
        {
            InitializeComponent();

            ((ChaptersPageViewModel)DataContext).PropertyChanged += new System.ComponentModel.PropertyChangedEventHandler(ChaptersPage_PropertyChanged);
        }

        void ChaptersPage_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName.Equals("NavigateTarget"))
            {
                NavigationService.Navigate(new Uri(((ChaptersPageViewModel)DataContext).NavigateTarget, UriKind.Relative));
            }
        }
    }
}