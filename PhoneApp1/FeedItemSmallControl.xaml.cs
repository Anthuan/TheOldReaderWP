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
using System.IO;

namespace PhoneApp1
{
    public partial class FeedItemSmallControl : UserControl
    {
        private bool m_bSelected = false;
        private Brush m_unselectedColor;
        private string m_id;
        public string ItemId { get { return m_id; } }
        public delegate void NavigateToRenameHander(object sender, EventArgs e);
        NavigateToRenameHander NavigateToRenameParentFunction;
        public delegate void NavigateToPreviewHander(object sender, EventArgs e);
        NavigateToPreviewHander NavigateToPreviewParentFunction;
        ItemType m_type;
        public ItemType FeedItemType { get { return m_type; } }

        public enum ItemType
        {
            ALL,
            FOLDER,
            SUBSCRIPTION
        }

        public FeedItemSmallControl()
        {
            InitializeComponent();
        }

        public FeedItemSmallControl(string faviconPath, string localFaviconPath, int unreadcount, string feedname, string id, NavigateToRenameHander navigateToRenameFunction, ItemType type, NavigateToPreviewHander navigateToPreviewFunction)
        {
            InitializeComponent();
            BitmapImage image;
            try
            {
                Console.WriteLine("Attempting to read " + localFaviconPath + " ...");
                image = new BitmapImage(new Uri(localFaviconPath, UriKind.Relative));
                if (image.DecodePixelHeight == 0 || image.DecodePixelWidth == 0) image = new BitmapImage(new Uri(faviconPath, UriKind.Absolute));
            }
            catch(Exception e)
            {
                if (faviconPath == null)
                {
                    image = new BitmapImage(new Uri(@"resources\favicon.png", UriKind.Relative));
                }
                else
                {
                    image = new BitmapImage(new Uri(faviconPath, UriKind.Absolute));
                }
            }
            ImgFavicon.Source = image;
            TxtUnreadCount.Text = unreadcount.ToString();
            TxtFeedname.Text = feedname;
            m_id = id;
            LayoutRoot.Background = new SolidColorBrush(Colors.Black);
            NavigateToRenameParentFunction = navigateToRenameFunction;
            NavigateToPreviewParentFunction = navigateToPreviewFunction;
            m_type = type;
        }

        private void FeedItemSmallControl_Tap(object sender, System.Windows.Input.GestureEventArgs e)
        {
            if (NavigateToPreviewParentFunction != null)
                this.NavigateToPreviewParentFunction(this, EventArgs.Empty);
        }

        private void ImgFavicon_Tap(object sender, System.Windows.Input.GestureEventArgs e)
        {
            if (m_bSelected)
            {
                LayoutRoot.Background = m_unselectedColor;
            }
            else
            {
                m_unselectedColor = LayoutRoot.Background;
                LayoutRoot.Background = new SolidColorBrush(Colors.Red);
            }
            m_bSelected = !m_bSelected;
        }

        private void markAllAsReadMenuItem(object sender, RoutedEventArgs e)
        {
            if(m_id != null)
                App.Current.TheOldReaderManager.MarkAllAsRead(m_id);
        }

        private void renameMenuItem(object sender, RoutedEventArgs e)
        {
            if(NavigateToRenameParentFunction != null)
                this.NavigateToRenameParentFunction(this, EventArgs.Empty);
        }

        private void unsubscribeMenuItem(object sender, RoutedEventArgs e)
        {
            if (m_id != null)
                App.Current.TheOldReaderManager.Unsubscribe(m_id);
        }
    }
}
