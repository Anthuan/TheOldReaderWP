using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Navigation;
using Microsoft.Phone.Controls;
using Microsoft.Phone.Shell;

namespace PhoneApp1
{
    public partial class FeedPreview : PhoneApplicationPage
    {
        FeedItemSmallControl.ItemType m_type;
        string m_itemId;

        public FeedPreview()
        {
            InitializeComponent();
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

        protected override void OnNavigatedTo(System.Windows.Navigation.NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);
            if (e.NavigationMode == NavigationMode.New)
            {
                try
                {
                    m_itemId = this.NavigationContext.QueryString["id"];
                    IEnumerable<TheOldReader.SubscriptionList.SubscriptionItem> feedItem = from TheOldReader.SubscriptionList.SubscriptionItem item in App.Current.TheOldReaderManager.Subscriptions.subscriptions
                                                                                           where item.id == m_itemId
                                                                                           select item;
                    if (feedItem.Count() > 0) // is it a subscription?
                    {
                        MainPageTitle.Text = "TheOldReader - " + feedItem.First().title;
                        m_type = FeedItemSmallControl.ItemType.SUBSCRIPTION;
                    }
                    else if (m_itemId.StartsWith("user/-/label/"))
                    {
                        MainPageTitle.Text = "TheOldReader - " + m_itemId.Substring(m_itemId.LastIndexOf("/")+1);
                        m_type = FeedItemSmallControl.ItemType.FOLDER;
                    }
                }
                catch (Exception eri)
                {
                    MainPageTitle.Text = "TheOldReader - All items";
                    m_type = FeedItemSmallControl.ItemType.ALL;
                }

                StartBar();
                LoadArticlePreview();
            }
        }

        private async void LoadArticlePreview()
        {
            switch(m_type)
            {
                case FeedItemSmallControl.ItemType.ALL:
                    ShowAllArticles();
                    break;
                case FeedItemSmallControl.ItemType.FOLDER:
                    LoadByFolder();
                    break;
                case FeedItemSmallControl.ItemType.SUBSCRIPTION:
                    LoadBySource();
                    break;
                default:
                    break;
            }
        }

        private async void ShowAllArticles()
        {
            TheOldReader.FeedArticleList fal = App.Current.TheOldReaderManager.FeedArticleElementList;

            for (int i = 0; i < fal.items.Count(); i++)
            {
                DateTime dt = new DateTime(fal.items[i].timestampUsec);
                string smalltext = dt.ToString("M") + " @ " + fal.items[i].origin.title;

                IEnumerable<TheOldReader.SubscriptionList.SubscriptionItem> feedItem = from TheOldReader.SubscriptionList.SubscriptionItem item in App.Current.TheOldReaderManager.Subscriptions.subscriptions
                                                                                       where item.title == fal.items[i].origin.title
                                                                                       select item;

                string sFaviconPath = "", sLocalFaviconPath = "" ;
                if(feedItem.Count() > 0)
                {
                    sFaviconPath = feedItem.First().iconUrl;
                    sLocalFaviconPath = feedItem.First().localIconUrl;
                }
                FeedItemLargeControl flc = new FeedItemLargeControl(sFaviconPath, sLocalFaviconPath, HttpUtility.HtmlDecode(fal.items[i].title), smalltext, fal.items[i].id, NavigateToArticle);
                ContentPanel.Children.Add(flc);
            }
            StopBar();
        }

        public async void NavigateToArticle(object sender, EventArgs e)
        {
            string id = ((FeedItemLargeControl)sender).ItemId;
            IEnumerable<TheOldReader.FeedArticleList.FeedArticleItem> feedItem = from TheOldReader.FeedArticleList.FeedArticleItem item in App.Current.TheOldReaderManager.FeedArticleElementList.items
                                                                                   where item.id == id
                                                                                   select item;
            if (feedItem.Count() > 0)
            {
                NavigationService.Navigate(new Uri("/Article.xaml?id=" + id, UriKind.Relative));
            }
        }

        private async void LoadByFolder()
        {
            TheOldReader.FeedArticleList fal = App.Current.TheOldReaderManager.FeedArticleElementList;

            IEnumerable<TheOldReader.FeedArticleList.FeedArticleItem> feedItem =
                from TheOldReader.FeedArticleList.FeedArticleItem item in fal.items 
                where item.categories.Contains(m_itemId)
                select item;

            foreach (TheOldReader.FeedArticleList.FeedArticleItem item in feedItem)
            {
                DateTime dt = new DateTime(item.timestampUsec);
                string smalltext = dt.ToString("M") + " @ " + item.origin.title;

                string sFaviconPath = "", sLocalFaviconPath = "";
                IEnumerable<TheOldReader.SubscriptionList.SubscriptionItem> feedSubItem = from TheOldReader.SubscriptionList.SubscriptionItem subitem in App.Current.TheOldReaderManager.Subscriptions.subscriptions
                                                                                       where subitem.title == item.origin.title
                                                                                       select subitem;
                if (feedItem.Count() > 0)
                {
                    sFaviconPath = feedSubItem.First().iconUrl;
                    sLocalFaviconPath = feedSubItem.First().localIconUrl;
                }
                FeedItemLargeControl flc = new FeedItemLargeControl(sFaviconPath, sLocalFaviconPath, HttpUtility.HtmlDecode(item.title), smalltext, item.id, NavigateToArticle);
                ContentPanel.Children.Add(flc);
            }
            StopBar();
        }

        private async void LoadBySource()
        {
            TheOldReader.FeedArticleList fal = App.Current.TheOldReaderManager.FeedArticleElementList;

            IEnumerable<TheOldReader.FeedArticleList.FeedArticleItem> feedItem =
                from TheOldReader.FeedArticleList.FeedArticleItem item in fal.items
                where item.origin.streamid == m_itemId
                select item;

            foreach (TheOldReader.FeedArticleList.FeedArticleItem item in feedItem)
            {
                DateTime dt = new DateTime(item.timestampUsec);
                string smalltext = dt.ToString("M") + " @ " + item.origin.title;

                string sFaviconPath = "", sLocalFaviconPath = "";
                IEnumerable<TheOldReader.SubscriptionList.SubscriptionItem> feedSubItem = from TheOldReader.SubscriptionList.SubscriptionItem subitem in App.Current.TheOldReaderManager.Subscriptions.subscriptions
                                                                                          where subitem.title == item.origin.title
                                                                                          select subitem;
                if (feedItem.Count() > 0)
                {
                    sFaviconPath = feedSubItem.First().iconUrl;
                    sLocalFaviconPath = feedSubItem.First().localIconUrl;
                }
                FeedItemLargeControl flc = new FeedItemLargeControl(sFaviconPath, sLocalFaviconPath, HttpUtility.HtmlDecode(item.title), smalltext, item.id, NavigateToArticle);
                ContentPanel.Children.Add(flc);
            }
            StopBar();
        }
    }
}