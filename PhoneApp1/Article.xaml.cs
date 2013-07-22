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
    public partial class Article : PhoneApplicationPage
    {
        string m_id;
        TheOldReader.FeedArticleList.FeedArticleItem m_article;
        public Article()
        {
            InitializeComponent();
        }

        protected override void OnNavigatedTo(System.Windows.Navigation.NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);
            try
            {
                m_id = this.NavigationContext.QueryString["id"];

                IEnumerable<TheOldReader.FeedArticleList.FeedArticleItem> feedItem = from TheOldReader.FeedArticleList.FeedArticleItem item in App.Current.TheOldReaderManager.FeedArticleElementList.items
                                                                                     where item.id == m_id
                                                                                     select item;
                if (feedItem.Count() > 0)
                {
                    m_article = feedItem.First();
                }

            }
            catch (Exception eri)
            {
            }

            StartBar();
            LoadArticle();
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

        private async void LoadArticle()
        {
            this.MainPageTitle.Text = "The Old Reader - " + m_article.title;
            MyBrowser.NavigateToString(m_article.summary.content);
            StopBar();
        }
    }
}