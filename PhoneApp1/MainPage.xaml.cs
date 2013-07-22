using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Navigation;
using Microsoft.Phone.Controls;
using Microsoft.Phone.Shell;
using PhoneApp1.Resources;
using System.Text;
using System.IO;
using System.Threading;
using System.Diagnostics;
using System.Threading.Tasks;


namespace PhoneApp1
{

    public partial class MainPage : PhoneApplicationPage
    {
        private bool bUnreadOnly = false;
        private bool bAllFeeds = false;
        private bool bReadyForPreview = false;
        private bool bSuccessOnFirstTry = true;
        public AutoResetEvent areFormLoaded;
        public AutoResetEvent areAuthenticationCompleted;

        public TheOldReader m_TheOldReaderManager;
        // Constructor
        public MainPage()
        {
            //Task<bool> a = AnthuanUtils.DeleteFile("token");
            //a.Wait();
            InitializeComponent();
            BuildLocalizedApplicationBar();
            areFormLoaded = new AutoResetEvent(false);
            areAuthenticationCompleted = new AutoResetEvent(false);
            ProgressBarObject.IsIndeterminate = true;
            PrepareMasterObject();
        }

        public async void PrepareMasterObject()
        {
            bool bTimedOut = false;
            Task<bool> a = AnthuanUtils.FileExists("token");
            await a;
            if (!bTimedOut && a.Result)
            {
                m_TheOldReaderManager = new TheOldReader(m_TheOldReaderManager_AuthenticationCompleted);
                m_TheOldReaderManager.DownloadToReadCompleted += TheOldReaderManager_DownloadToReadCompleted;
                m_TheOldReaderManager.DownloadStarted += m_TheOldReaderManager_DownloadStarted;
                m_TheOldReaderManager.ArticleDownloadCompleted += m_TheOldReaderManager_ArticleDownloadCompleted;
                App.Current.TheOldReaderManager = m_TheOldReaderManager;
                StartBar();
            }
            else
            {
                DoNothing();
            }
        }

        void DoNothing()
        {
            bSuccessOnFirstTry = false;
            DebugOutput.TextWrapping = TextWrapping.Wrap;
            DebugOutput.Text = "Click on the settings icon on the application bar to set up credentials!";
            StopBar();
        }

        async void m_TheOldReaderManager_AuthenticationCompleted(object sender, EventArgs e)
        {
            areFormLoaded.WaitOne();
            areAuthenticationCompleted.Set();
            if (((TheOldReader)sender).IsAuthenticated == false)
            {
                try
                {
                    bSuccessOnFirstTry = false;
                    areAuthenticationCompleted.Reset();
                    Dispatcher.BeginInvoke(new Action(() => appBarButtonSettings_Click(this, EventArgs.Empty)));
                }
                catch (Exception err)
                {
                    Console.WriteLine(err.Message + " " + err.InnerException.Message);
                }
            }
            else
            {
                Dispatcher.BeginInvoke(m_TheOldReaderManager.DownloadToRead);
            }
        }

        protected override void OnNavigatedTo(System.Windows.Navigation.NavigationEventArgs e)
        {
            areFormLoaded.Set();
            base.OnNavigatedTo(e);
            if (e.NavigationMode == NavigationMode.Back)
            {
                if (m_TheOldReaderManager != null)
                {
                    StartBar();
                    m_TheOldReaderManager.DownloadToRead();
                }
                else
                {
                    if (!bSuccessOnFirstTry)
                    {
                        DebugOutput.Text = "Logging in...";
                        Dispatcher.BeginInvoke(PrepareMasterObject);
                    }
                }
            }
        }

        void m_TheOldReaderManager_ArticleDownloadCompleted(object sender, EventArgs e)
        {
            bReadyForPreview = true;
        }

        void m_TheOldReaderManager_DownloadStarted(object sender, EventArgs e)
        {
            try
            {
                Dispatcher.BeginInvoke(StartBar);
            }
            catch (Exception err)
            {
                Console.WriteLine(err.Message + " " + err.InnerException.Message);
            }
        }

        private async void StopBar()
        {
            ProgressBarObject.IsIndeterminate = false;
            ProgressBarObject.Visibility = System.Windows.Visibility.Collapsed;
        }

        private async void StartBar()
        {
            ProgressBarObject.IsIndeterminate = true;
            ProgressBarObject.Visibility = System.Windows.Visibility.Visible;
        }

        void RefreshContent()
        {
            bool bShow;
            ContentPanel.Children.Clear();
            DebugOutput.Visibility = System.Windows.Visibility.Collapsed;
            IList<TheOldReader.SubscriptionList.SubscriptionItem> si = m_TheOldReaderManager.Subscriptions.subscriptions;
            IList<TheOldReader.UnreadCount.UnreadItem> ui = m_TheOldReaderManager.UnreadItemCount.unreadcounts;
            for (int i = 0; i < ui.Count; i++)
            {
                IEnumerable<TheOldReader.SubscriptionList.SubscriptionItem> feedItem = from TheOldReader.SubscriptionList.SubscriptionItem item in si
                        where item.id == ui[i].id
                        select item;

                if (ui[i].id == "user/-/state/com.google/reading-list")
                {
                    MainPageTitle.Text = "The Old Reader (" + ui[i].count.ToString() + ")";
                    FeedItemSmallControl fi = new FeedItemSmallControl(null, null, ui[i].count, "All", null, null, FeedItemSmallControl.ItemType.ALL, NavigateToPreview);
                    fi.HorizontalAlignment = System.Windows.HorizontalAlignment.Stretch;
                    ContentPanel.Children.Add(fi);
                }
                else if (feedItem.Count() > 0) // subscription
                {
                    bShow = true;
                    TheOldReader.SubscriptionList.SubscriptionItem fitem = feedItem.First();

                    if ( !bAllFeeds && fitem.categories.Count > 0)
                    {
                        bShow = false;
                    }

                    if (bUnreadOnly && ui[i].count == 0)// unread only
                    {
                        bShow = false;
                    }
                    
                    if(bShow)
                    {
                        FeedItemSmallControl fi = new FeedItemSmallControl(fitem.iconUrl, fitem.localIconUrl, ui[i].count, fitem.title, fitem.id, NavigateToRename, FeedItemSmallControl.ItemType.SUBSCRIPTION, NavigateToPreview);
                        fi.HorizontalAlignment = System.Windows.HorizontalAlignment.Stretch;
                        ContentPanel.Children.Add(fi);
                    }
                }
                else if (feedItem.Count() == 0) // folder
                {
                    bShow = true;
                    if (bUnreadOnly && ui[i].count == 0)
                    {
                        bShow = false;
                    }

                    if (bShow)
                    {
                        FeedItemSmallControl fi = new FeedItemSmallControl(null, null, ui[i].count, ui[i].id.Substring(ui[i].id.LastIndexOf("/") + 1), ui[i].id, NavigateToRename, FeedItemSmallControl.ItemType.FOLDER, NavigateToPreview);
                        fi.HorizontalAlignment = System.Windows.HorizontalAlignment.Stretch;
                        ContentPanel.Children.Add(fi);
                    }
                }
            }
            StopBar();
        }

        void TheOldReaderManager_DownloadToReadCompleted(object sender, EventArgs e)
        {
            try
            {
                Dispatcher.BeginInvoke(RefreshContent);
            }
            catch (Exception err)
            {
                Console.WriteLine(err.Message + " " + err.InnerException.Message);
            }
        }

        private void BuildLocalizedApplicationBar()
        {
            ApplicationBar = new ApplicationBar();

            ApplicationBarIconButton appBarButtonSync = new ApplicationBarIconButton(new Uri("/Resources/sync.png", UriKind.Relative));
            appBarButtonSync.Text = "Sync";
            appBarButtonSync.Click += appBarButtonSync_Click;
            ApplicationBar.Buttons.Add(appBarButtonSync);

            ApplicationBarIconButton appBarButtonSettings = new ApplicationBarIconButton(new Uri("/Resources/feature.settings.png", UriKind.Relative));
            appBarButtonSettings.Text = "Settings";
            appBarButtonSettings.Click += appBarButtonSettings_Click;
            ApplicationBar.Buttons.Add(appBarButtonSettings);

            ApplicationBarIconButton appBarButtonNew = new ApplicationBarIconButton(new Uri("/Resources/new.png", UriKind.Relative));
            appBarButtonNew.Text = "New";
            appBarButtonNew.Click += appBarButtonNew_Click;
            ApplicationBar.Buttons.Add(appBarButtonNew);

            ApplicationBarIconButton appBarButtonMarkAllAsRead = new ApplicationBarIconButton(new Uri("/Resources/check.png", UriKind.Relative));
            appBarButtonMarkAllAsRead.Text = "all read";
            appBarButtonMarkAllAsRead.Click += appBarButtonMarkAllAsRead_Click;
            ApplicationBar.Buttons.Add(appBarButtonMarkAllAsRead);

            //ApplicationBarIconButton appBarButtonSearch = new ApplicationBarIconButton(new Uri("/Resources/feature.search.png", UriKind.Relative));
            //appBarButtonSearch.Text = "Search";
            //ApplicationBar.Buttons.Add(appBarButtonSearch);

            ApplicationBar.IsMenuEnabled = true;
            ApplicationBarMenuItem appBarMenuItemToggleUnreadOnly = new ApplicationBarMenuItem("toggle unread only");
            appBarMenuItemToggleUnreadOnly.Click += appBarMenuItemToggleUnreadOnly_Click;
            ApplicationBar.MenuItems.Add(appBarMenuItemToggleUnreadOnly);

            ApplicationBarMenuItem appBarMenuItemToggleAllFeeds = new ApplicationBarMenuItem("toggle all feeds");
            appBarMenuItemToggleAllFeeds.Click += appBarMenuItemToggleAllFeeds_Click;
            ApplicationBar.MenuItems.Add(appBarMenuItemToggleAllFeeds);
        }

        void appBarButtonMarkAllAsRead_Click(object sender, EventArgs e)
        {
            if(MessageBox.Show("Are you sure?", "MARK EVERYTHING AS READ", MessageBoxButton.OKCancel) == MessageBoxResult.OK)
                m_TheOldReaderManager.MarkAllAsRead();
        }

        void appBarMenuItemToggleAllFeeds_Click(object sender, EventArgs e)
        {
            StartBar();
            bAllFeeds = !bAllFeeds;
            RefreshContent();
        }

        void appBarMenuItemToggleUnreadOnly_Click(object sender, EventArgs e)
        {
            StartBar();
            bUnreadOnly = !bUnreadOnly;
            RefreshContent();
        }

        void appBarButtonNew_Click(object sender, EventArgs e)
        {
            MessageBox.Show("If only I could .GetText from the clipboard!");
        }

        void appBarButtonSettings_Click(object sender, EventArgs e)
        {
            NavigationService.Navigate(new Uri("/Settings.xaml", UriKind.Relative));
        }

        void NavigateToRename(object sender, EventArgs e)
        {
            NavigationService.Navigate(new Uri("/RenameItem.xaml?id=" + ((FeedItemSmallControl)sender).ItemId.ToString() + "&type=" + ((FeedItemSmallControl)sender).FeedItemType.ToString(), UriKind.Relative));
        }

        void NavigateToPreview(object sender, EventArgs e)
        {
            if (!bReadyForPreview)
            {
                MessageBox.Show("Article download still in progress...");
                return;
            }
            if( ((FeedItemSmallControl)sender).ItemId != null )
                NavigationService.Navigate(new Uri("/FeedPreview.xaml?id=" + ((FeedItemSmallControl)sender).ItemId, UriKind.Relative));
            else
                NavigationService.Navigate(new Uri("/FeedPreview.xaml", UriKind.Relative));
        }

        void appBarButtonSync_Click(object sender, EventArgs e)
        {
            m_TheOldReaderManager.DownloadToRead();           
            ProgressBarObject.Visibility = System.Windows.Visibility.Visible;
            ProgressBarObject.IsIndeterminate = true;
        }
    }
}