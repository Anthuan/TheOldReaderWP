using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Navigation;
using Microsoft.Phone.Controls;
using Microsoft.Phone.Shell;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace PhoneApp1
{
    public partial class FeedItemLargeControl : UserControl
    {
        private bool m_bRead = false;
        private bool m_bSelected = false;
        private Brush m_unselectedColor;
        private string m_id;
        public string ItemId { get { return m_id; } }
        public delegate void NavigateToArticleHander(object sender, EventArgs e);
        NavigateToArticleHander NavigateToArticleParentFunction;

        public FeedItemLargeControl()
        {
            InitializeComponent();
            m_id = null;
        }

        public FeedItemLargeControl(string faviconImagePath, string faviconLocalImagePath, string LargeText, string SmallText, string id, NavigateToArticleHander NavigateToArticleFunction)
        {
            InitializeComponent();
            TxtFeedname.Text = LargeText;
            TxtExtraInfo.Text = SmallText;

            BitmapImage image;
            try
            {
                image = new BitmapImage(new Uri(faviconLocalImagePath, UriKind.Relative));
                if (image.DecodePixelHeight == 0 || image.DecodePixelWidth == 0) image = new BitmapImage(new Uri(faviconImagePath, UriKind.Absolute));
            }
            catch (Exception e)
            {
                if (faviconImagePath == null)
                {
                    image = new BitmapImage(new Uri(@"resources\favicon.png", UriKind.Relative));
                }
                else
                {
                    image = new BitmapImage(new Uri(faviconImagePath, UriKind.Absolute));
                }
            }
            ImgFavicon.Source = image;
            m_id = id;
            this.NavigateToArticleParentFunction = NavigateToArticleFunction;
        }

        private void TxtFeedname_Tap(object sender, System.Windows.Input.GestureEventArgs e)
        {
            if (NavigateToArticleParentFunction != null)
            {
                SetRead();
                NavigateToArticleParentFunction(this, EventArgs.Empty);
            }
        }

        private void MarkReadStatusMenuItem_Click_1(object sender, RoutedEventArgs e)
        {
            SetRead();
        }

        private void MarkUnreadStatusMenuItem_Click(object sender, RoutedEventArgs e)
        {
            SetUnread();
        }

        public async void SetRead()
        {
            App.Current.TheOldReaderManager.MarkItemRead(m_id);
            this.m_bRead = true;
            TxtFeedname.Foreground = new SolidColorBrush(Color.FromArgb(255, 0, 104, 208));
        }

        public async void SetUnread()
        {
            App.Current.TheOldReaderManager.MarkItemUnread(m_id);
            this.m_bRead = false;
            TxtFeedname.Foreground = new SolidColorBrush(Color.FromArgb(255, 255, 255, 255));
        }
    }
}
