using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using Microsoft.Phone.Controls;
using Microsoft.Phone.Shell;
using System.IO;
using System.IO.IsolatedStorage;
using System.Text;

namespace MangaStream
{
    public partial class ViewMangaPage : PhoneApplicationPage
    {
        public ViewMangaPage()
        {
            InitializeComponent();

            HideJumpToPageOverlay(new EventHandler(DoNothingEventHandler));
        }

        void ViewMangaPage_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName.Equals("DisplayContent"))
            {
                webBrowser.Navigate(new Uri("index.html", UriKind.Relative));
            }
            else if (e.PropertyName.Equals("PreviousAllowed"))
            {
                ((ApplicationBarIconButton)ApplicationBar.Buttons[0]).IsEnabled = ((ViewMangaPageModel)DataContext).PreviousAllowed;
            }
            else if (e.PropertyName.Equals("NextAllowed"))
            {
                ((ApplicationBarIconButton)ApplicationBar.Buttons[1]).IsEnabled = ((ViewMangaPageModel)DataContext).NextAllowed;
            }
        }

        private void PreviousButton_Click(object sender, EventArgs e)
        {
            if (!((ViewMangaPageModel)DataContext).Loading)
            {
                HideJumpToPageOverlay(new EventHandler(PreviousEventHandler));
            }
        }

        private void PreviousEventHandler(object sender, EventArgs e)
        {
            ((ViewMangaPageModel)DataContext).OnPreviousClicked();
        }

        private void NextButton_Click(object sender, EventArgs e)
        {
            if (!((ViewMangaPageModel)DataContext).Loading)
            {
                HideJumpToPageOverlay(new EventHandler(NextEventHandler));
            }
        }

        private void NextEventHandler(object sender, EventArgs e)
        {
            ((ViewMangaPageModel)DataContext).OnNextClicked();
        }

        private void RefreshMenu_Click(object sender, EventArgs e)
        {
            if (!((ViewMangaPageModel)DataContext).Loading)
            {
                HideJumpToPageOverlay(new EventHandler(RefreshEventHandler));
            }
        }

        private void RefreshEventHandler(object sender, EventArgs e)
        {
            ((ViewMangaPageModel)DataContext).OnRefresh();
        }

        private void DownloadAllMenu_Click(object sender, EventArgs e)
        {
            if (!((ViewMangaPageModel)DataContext).Loading)
            {
                HideJumpToPageOverlay(new EventHandler(DownloadAllEventHandler));
            }
        }

        private void DownloadAllEventHandler(object sender, EventArgs e)
        {
            ((ViewMangaPageModel)DataContext).OnDownloadAll();
        }

        private void JumpToPageMenu_Click(object sender, EventArgs e)
        {
            ShowJumpToPageOverlay();
        }

        private void ViewMangaPage_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            // TODO: Is there a way to know how big the header is and then compensate in the height?
            JumpToPageOverlay.Width = e.NewSize.Width;
            JumpToPageOverlay.Height = e.NewSize.Height - 62;
            ListBox.Width = e.NewSize.Width;
            ListBox.Height = e.NewSize.Height - 62;
        }

        private void ListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (e.AddedItems.Count == 1 && ListBox.SelectedItem != null)
            {
                HideJumpToPageOverlayAfterSelection(ListBox.SelectedIndex);
                ListBox.SelectedItem = null;
            }
        }

        private void ViewMangaPage_BackKeyPress(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (JumpToPageOverlay.Visibility == Visibility.Visible)
            {
                HideJumpToPageOverlay(new EventHandler(DoNothingEventHandler));
                e.Cancel = true;
            }
        }

        private void DoNothingEventHandler(object sender, EventArgs e)
        {
        }

        private void ShowJumpToPageOverlay()
        {
            JumpToPageOverlay.Visibility = Visibility.Visible;

            SwivelTransitionMode swivelTransitionMode = (SwivelTransitionMode)Enum.Parse(typeof(SwivelTransitionMode), "FullScreenIn", false);
            TransitionElement transitionElement = new SwivelTransition { Mode = swivelTransitionMode };
            ITransition transition = transitionElement.GetTransition(Pivot);
            transition.Completed += delegate
            {
                transition.Stop();
            };
            transition.Begin();
        }

        private void HideJumpToPageOverlay(EventHandler eventHandler)
        {
            SwivelTransitionMode swivelTransitionMode = (SwivelTransitionMode)Enum.Parse(typeof(SwivelTransitionMode), "FullScreenOut", false);
            TransitionElement transitionElement = new SwivelTransition { Mode = swivelTransitionMode };
            ITransition transition = transitionElement.GetTransition(Pivot);
            transition.Completed += delegate
            {
                transition.Stop();
                JumpToPageOverlay.Visibility = Visibility.Collapsed;
                eventHandler.Invoke(this, new EventArgs());
            };
            transition.Begin();

            if (((ViewMangaPageModel)DataContext).Pages.Count > 0)
            {
                ListBox.ScrollIntoView(((ViewMangaPageModel)DataContext).Pages[0]);
            }
        }

        private void HideJumpToPageOverlayAfterSelection(int index)
        {
            SwivelTransitionMode swivelTransitionMode = (SwivelTransitionMode)Enum.Parse(typeof(SwivelTransitionMode), "FullScreenOut", false);
            TransitionElement transitionElement = new SwivelTransition { Mode = swivelTransitionMode };
            ITransition transition = transitionElement.GetTransition(Pivot);
            transition.Completed += delegate
            {
                transition.Stop();
                JumpToPageOverlay.Visibility = Visibility.Collapsed;
                ((ViewMangaPageModel)DataContext).JumpToPage(index);
            };
            transition.Begin();

            if (((ViewMangaPageModel)DataContext).Pages.Count > 0)
            {
                ListBox.ScrollIntoView(((ViewMangaPageModel)DataContext).Pages[0]);
            }
        }
    }
}