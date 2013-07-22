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
    public partial class RenameItem : PhoneApplicationPage
    {
        string m_itemid;
        FeedItemSmallControl.ItemType m_type;

        public RenameItem()
        {
            InitializeComponent();
        }

        protected override void OnNavigatedTo(System.Windows.Navigation.NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);
            m_itemid = this.NavigationContext.QueryString["id"];
            IEnumerable<TheOldReader.SubscriptionList.SubscriptionItem> feedItem = from TheOldReader.SubscriptionList.SubscriptionItem item in App.Current.TheOldReaderManager.Subscriptions.subscriptions
                                                                                   where item.id == m_itemid
                                                                                   select item;
            if (feedItem.Count() > 0) // is it a subscription?
            {
                OldItemName.Text = feedItem.First().title;
                m_type = FeedItemSmallControl.ItemType.SUBSCRIPTION;
            }
            else
            {
                OldItemName.Text = m_itemid.Substring(m_itemid.LastIndexOf("/") + 1);
                m_type = FeedItemSmallControl.ItemType.FOLDER;
            }
        }

        private void ButtonRenameItem_Click_1(object sender, RoutedEventArgs e)
        {
            if (NewItemName.Text.Length > 0)
            {
                if (m_type == FeedItemSmallControl.ItemType.SUBSCRIPTION)
                {
                    // TODO: Implement me
                }
                else if (m_type == FeedItemSmallControl.ItemType.FOLDER)
                {
                    App.Current.TheOldReaderManager.RenameFolder(OldItemName.Text, NewItemName.Text);
                    NavigationService.GoBack();
                }
            }
        }
    }
}