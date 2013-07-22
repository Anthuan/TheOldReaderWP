using System;
using System.Collections.Generic;
using System.IO.IsolatedStorage;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using System.Threading;
using System.Net;
using Windows.Storage;

namespace PhoneApp1
{
    public class TheOldReader
    {
        private string m_Token;
        private bool m_isAuthenticated;
        private bool m_AuthenticationInProgress;
        public bool IsAuthenticated { get { return m_isAuthenticated; } }
        public bool AuthenticationInProgress { get { return m_AuthenticationInProgress; } }
        private string m_Error;
        public string LastError { get { return m_Error; } }
        UnreadCount m_uc;
        public UnreadCount UnreadItemCount { get { return m_uc; } }
        SubscriptionList m_sl;
        public SubscriptionList Subscriptions { get { return m_sl; } }
        FolderList m_f;
        public FeedItemElementList FeedIdsElementList { get { return m_fil; } }
        FeedItemElementList m_fil;
        FeedArticleList m_fal;
        public FeedArticleList FeedArticleElementList { get { return m_fal; } }
        public FolderList Folders { get { return m_f; } }
        public delegate void DownloadStartedHandler(object sender, EventArgs e);
        public event DownloadStartedHandler DownloadStarted;
        public delegate void AuthenticationCompletedHandler(object sender, EventArgs e);
        public event AuthenticationCompletedHandler AuthenticationCompleted;
        public delegate void DownloadToReadHandler(object sender, EventArgs e);
        public event DownloadToReadHandler DownloadToReadCompleted;
        public delegate void ArticleDownloadHandler(object sender, EventArgs e);
        public event ArticleDownloadHandler ArticleDownloadCompleted;
        private Semaphore m_TokenReady;
        private Semaphore m_articleReady;
        private CountdownEvent m_UnreadProcessReady;
        private Dictionary<string, string> m_feedFavicon;
        private ManualResetEvent areAuthentication;

        private async static void SaveStaticToken(string output)
        {
            string token;
            if (output != null)
            {
                if (output.IndexOf("Auth=") != -1)
                {
                    token = "GoogleLogin " + output.Substring(output.IndexOf("Auth=")).Replace("Auth=", "auth=");
                    if (token.IndexOf("\n") != -1)
                    {
                        token = token.Substring(0, token.IndexOf("\n"));
                    }
                    AnthuanUtils.WriteFile("token", token);
                }
            }
        }

        private void GetToken(string output)
        {
            m_Token = null;
            if (output != null)
            {
                if (output.IndexOf("Auth=") != -1)
                {
                    m_Token = "GoogleLogin " + output.Substring(output.IndexOf("Auth=")).Replace("Auth=", "auth=");
                    if (m_Token.IndexOf("\n") != -1)
                    {
                        m_Token = m_Token.Substring(0, m_Token.IndexOf("\n"));
                    }
                    m_isAuthenticated = true;
                    m_AuthenticationInProgress = false;
                    AnthuanUtils.WriteFile("token", m_Token);
                    areAuthentication.Set();
                    m_TokenReady.Release();
                }
                else
                {
                    m_AuthenticationInProgress = false;
                    m_Error = "No authorization token!\r\n" + output;
                }
            }
            else
            {
                m_AuthenticationInProgress = false;
                m_Error = "No output from authorization request!";
            }
            if(AuthenticationCompleted != null) AuthenticationCompleted(this, EventArgs.Empty);
        }

        public TheOldReader()
        {
            areAuthentication = new ManualResetEvent(false);
            m_isAuthenticated = false;
            m_Error = "";
            m_TokenReady = new Semaphore(0, 1);
            m_articleReady = new Semaphore(0, 1);
            Authenticate();
        }

        public TheOldReader(AuthenticationCompletedHandler AuthenticationCompletedEvent)
        {
            areAuthentication = new ManualResetEvent(false);
            m_isAuthenticated = false;
            m_Error = "";
            m_TokenReady = new Semaphore(0, 1);
            m_articleReady = new Semaphore(0, 1);
            this.AuthenticationCompleted = AuthenticationCompletedEvent;
            Authenticate();
        }

        public TheOldReader(string username, string password)
        {
            areAuthentication = new ManualResetEvent(false);
            m_isAuthenticated = false;
            m_TokenReady = new Semaphore(0, 1);
            m_articleReady = new Semaphore(0, 1);
            m_Error = "";
            Authenticate(username, password);
        }

        public TheOldReader(string username, string password, AuthenticationCompletedHandler AuthenticationCompletedEvent)
        {
            areAuthentication = new ManualResetEvent(false);
            m_isAuthenticated = false;
            m_TokenReady = new Semaphore(0, 1);
            m_articleReady = new Semaphore(0, 1);
            m_Error = "";
            Authenticate(username, password, AuthenticationCompletedEvent);
        }

        public async void Authenticate()
        {
            try
            {
                m_AuthenticationInProgress = true;
                m_Token = await AnthuanUtils.ReadFile("token");
                m_AuthenticationInProgress = false;
                m_isAuthenticated = true;
                m_TokenReady.Release();
            }
            catch (Exception e)
            {
                m_Token = null;
                m_isAuthenticated = false;
            }
            if (AuthenticationCompleted != null) 
            {
                new Thread(() => AuthenticationCompleted(this, EventArgs.Empty)).Start();
            }
        }

        public async void DownloadToRead()
        {
            if (m_AuthenticationInProgress) m_TokenReady.WaitOne();
            if (m_isAuthenticated)
            {
                if (DownloadStarted != null) DownloadStarted(this, EventArgs.Empty);
                m_UnreadProcessReady = new CountdownEvent(3);
                try
                {
                    System.Diagnostics.Debug.WriteLine("BEGINNING DOWNLOAD OF ARTICLES!");
                    GetUnreadCount();
                    GetSubscriptionList();
                    GetFolders();
                    GetItemIdsAll(ItemListingType.ONLYUNREAD);
                }
                catch (Exception ed)
                {
                    Console.WriteLine(ed.Message);
                }
            }
        }

        public async void Authenticate(string username, string password)
        {
            string url = "https://theoldreader.com/reader/api/0/accounts/ClientLogin";
            string post = "client=WPReader&accountType=HOSTED&service=reader&Email=" + username + "&Passwd=" + password;
            m_AuthenticationInProgress = true;
            AnthuanUtils.Post(url, post, GetToken);
        }

        public async static void StaticAuthenticate(string username, string password)
        {
            string url = "https://theoldreader.com/reader/api/0/accounts/ClientLogin";
            string post = "client=AnthuanWPReader&accountType=HOSTED&service=reader&Email=" + username + "&Passwd=" + password;
            AnthuanUtils.Post(url, post, SaveStaticToken);            
        }

        public async void Authenticate(string username, string password, AuthenticationCompletedHandler AuthenticationCompleted)
        {
            string url = "https://theoldreader.com/reader/api/0/accounts/ClientLogin";
            string post = "client=AnthuanWPReader&accountType=HOSTED&service=reader&Email=" + username + "&Passwd=" + password;
            m_AuthenticationInProgress = true;
            AnthuanUtils.Post(url, post, GetToken);
            areAuthentication.WaitOne();
            AuthenticationCompleted(this, EventArgs.Empty);
        }

        public class UnreadCount
        {
            public class UnreadItem
            {
                public string id { get; set; }
                public int count { get; set; }
                public long newestItemTimestampUsec { get; set; }
            }
            public int max { get; set; }
            public IList<UnreadItem> unreadcounts { get; set; }
        }

        private void SetUnreadCount(string output)
        {
            if (output != null)
            {
                try
                {
                    m_uc = JsonConvert.DeserializeObject<UnreadCount>(output);
                    if (m_UnreadProcessReady.Signal()) DownloadToReadCompleted(this, EventArgs.Empty);
                }
                catch (Exception e)
                {
                    Console.WriteLine("UNREAD: " + e.Message);
                    Console.WriteLine("UNREAD: " + output);
                }
            }
            else
            {
                m_Error = "SetUnreadCount returned null";
            }
        }

        public void GetUnreadCount()
        {
            AnthuanUtils.Get("https://theoldreader.com/reader/api/0/unread-count?output=json", this.m_Token, SetUnreadCount);
        }

        public class SubscriptionList
        {
            public class SubscriptionItem
            {
                public class SubscriptionCategory
                {
                    public string id { get; set; }
                    public string label { get; set; }
                }
                public string id { get; set; }
                public string title { get; set; }
                public IList<SubscriptionCategory> categories { get; set; }
                public string sortid { get; set; }
                public long firstitemsec { get; set; }
                public string url { get; set; }
                public string htmlUrl { get; set; }
                public string iconUrl { get; set; }
                public string localIconUrl { get; set; }
            }
            public IList<SubscriptionItem> subscriptions { get; set; }
        }

        private void FixFavicons()
        {
            for (int i = 0; i < m_sl.subscriptions.Count; i++)
            {
                string domainonly;
                domainonly = m_sl.subscriptions[i].htmlUrl;
                if (domainonly.IndexOf("://") != -1) domainonly = domainonly.Substring(domainonly.IndexOf("://") + 3);
                if (domainonly.IndexOf("/") != -1) domainonly = domainonly.Substring(0, domainonly.IndexOf("/"));
                m_sl.subscriptions[i].iconUrl = "http://www.google.com/s2/favicons?domain=" + domainonly;
                m_sl.subscriptions[i].localIconUrl = m_sl.subscriptions[i].id.Replace("/", "") + ".png";
            }
        }

        private void SetSubstriptionList(string output)
        {
            if (output != null)
            {
                try
                {
                    m_sl = JsonConvert.DeserializeObject<SubscriptionList>(output);
                    FixFavicons();
                    DownloadFavicons();
                    if (m_UnreadProcessReady.Signal()) DownloadToReadCompleted(this, EventArgs.Empty);
                }
                catch (Exception e)
                {
                    Console.WriteLine("SUBSCRIPTION: " + e.Message);
                    Console.WriteLine("SUBSCRIPTION: " + output);
                }
            }
            else
            {
                m_Error = "SetSubscriptionList returned null";
            }
        }

        public void GetSubscriptionList()
        {
            AnthuanUtils.Get("https://theoldreader.com/reader/api/0/subscription/list?output=json", this.m_Token, SetSubstriptionList);           
        }

        void client_OpenReadCompleted(object sender, OpenReadCompletedEventArgs e)
        {
            IsolatedStorageFile file = IsolatedStorageFile.GetUserStoreForApplication();

            try
            {
                using (IsolatedStorageFileStream stream = new IsolatedStorageFileStream(e.UserState.ToString(), System.IO.FileMode.Create, file))
                {
                    byte[] buffer = new byte[1024];
                    while (e.Result.Read(buffer, 0, buffer.Length) > 0)
                    {
                        stream.Write(buffer, 0, buffer.Length);
                    }
                    stream.Close();
                    Console.WriteLine(e.UserState.ToString() + " created successfully!");
                }
            }
            catch (Exception erm)
            {
                Console.WriteLine(erm.Message);
            }
        }

        private async void DownloadFavicons()
        {
            try
            {
                string localfilename;
                m_feedFavicon = new Dictionary<string, string>();
                for (int i = 0; i < m_sl.subscriptions.Count; i++)
                {

                    localfilename = m_sl.subscriptions[i].id.Replace("/", "") + ".png";
                    m_sl.subscriptions[i].localIconUrl = localfilename;

                    WebClient client = new WebClient();
                    Uri uriIcon = null;
                    client.OpenReadCompleted += new OpenReadCompletedEventHandler(client_OpenReadCompleted);
                    try
                    {
                        m_feedFavicon.Add(m_sl.subscriptions[i].id, localfilename);
                        uriIcon = new Uri(m_sl.subscriptions[i].iconUrl, UriKind.Absolute);
                        client.OpenReadAsync(uriIcon, localfilename);
                    }
                    catch (Exception er)
                    {
                        Console.WriteLine(er.Message);
                    }
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }
        }

        public class FolderList
        {
            public class TagItem
            {
                public string id;
                public string sortid;
            }
            public IList<TagItem> tags;
        }

        private void SetFolderList(string output)
        {
            if (output != null)
            {
                try
                {
                    m_f = JsonConvert.DeserializeObject<FolderList>(output);
                    if (m_UnreadProcessReady.Signal()) DownloadToReadCompleted(this, EventArgs.Empty);
                }
                catch (Exception e)
                {
                    Console.WriteLine("FOLDER: " + e.Message);
                    Console.WriteLine("FOLDER: " + output);
                }
            }
            else
            {
                m_Error = "SetFolderList returned null";
            }
        }

        public void GetFolders()
        {
            AnthuanUtils.Get("https://theoldreader.com/reader/api/0/tag/list?output=json", this.m_Token, SetFolderList);
        }

        public void SubscriptionsChanged(string output)
        {
            DownloadToRead();
        }

        public void RenameFolder(string oldname, string newname)
        {
            string sRenameCommand = "s=user/-/label/" + HttpUtility.UrlEncode(oldname) + "&dest=user/-/label/" + HttpUtility.UrlEncode(newname);
            AnthuanUtils.Post("https://theoldreader.com/reader/api/0/rename-tag", this.m_Token, sRenameCommand, SubscriptionsChanged);
        }

        public void DeleteFolder(string name)
        {
            string sDeleteCommand = "s=user/-/label/" + HttpUtility.UrlEncode(name);
            AnthuanUtils.Post("https://theoldreader.com/reader/api/0/disable-tag", this.m_Token, sDeleteCommand, SubscriptionsChanged);
        }

        public void Subscribe(string address)
        {
            string sSafeAddress = HttpUtility.UrlEncode(address);
            AnthuanUtils.Post("https://theoldreader.com/reader/api/0/subscription/quickadd?quickadd=" + sSafeAddress, this.m_Token, SubscriptionsChanged);
        }

        public void Unsubscribe(string id)
        {
            string sUnsubscribeCommand = "ac=unsubscribe&s=" + id;
            AnthuanUtils.Post("https://theoldreader.com/reader/api/0/subscription/edit", this.m_Token, sUnsubscribeCommand, SubscriptionsChanged);
        }

        public void MarkAllAsRead()
        {
            string sMarkAsReadCommand = "s=user/-/state/com.google/reading-list";
            AnthuanUtils.Post("https://theoldreader.com/reader/api/0/mark-all-as-read", this.m_Token, sMarkAsReadCommand, SubscriptionsChanged);
        }

        public void MarkAllAsRead(string scopeid)
        {
            string sMarkAsReadCommand = "s=" + scopeid;
            AnthuanUtils.Post("https://theoldreader.com/reader/api/0/mark-all-as-read", this.m_Token, sMarkAsReadCommand, SubscriptionsChanged);
        }

        public void MarkItemUnread(string itemid)
        {
            string sMarkAsUnreadCommand = "r=user/-/state/com.google/read&i=" + itemid;
            AnthuanUtils.Post("https://theoldreader.com/reader/api/0/edit-tag", this.m_Token, sMarkAsUnreadCommand, null);
        }

        public void MarkItemRead(string itemid)
        {
            string sMarkAsReadCommand = "a=user/-/state/com.google/read&i=" + itemid;
            AnthuanUtils.Post("https://theoldreader.com/reader/api/0/edit-tag", this.m_Token, sMarkAsReadCommand, null);
        }

        public enum ItemListingType
        {
            ONLYUNREAD,
            ONLYREAD,
            ALL
        }

        public class FeedItemElementList
        {
            public class FeedItemElement
            {
                public string id;
                public string directstreamids;
                public long timestampusec;
            }
            public IList<FeedItemElement> itemrefs;
        }

        private void SetItemIds(string output)
        {
            if (output != null)
            {
                output = output.Replace("\"directStreamIds\":[],",""); // TODO: Follow up with the old reader team!
                try
                {
                    m_fil = JsonConvert.DeserializeObject<FeedItemElementList>(output);
                    GetItemContents(m_fil.itemrefs);
                }
                catch (Exception e)
                {
                    Console.WriteLine("FEED ITEM: " + e.Message);
                    Console.WriteLine("FEED ITEM: " + output);
                }
            }
            else
            {
                m_Error = "SetItemIds returned null";
            }
        }

        public class FeedArticleList
        {
            public string direction;
            public string id;
            public string title;
            public string description;
            public class FeedArticleListSelf
            {
                public string href;
            }
            FeedArticleListSelf self;
            public int updated;
            public class FeedArticleItem
            {
                public long crawlTimeMsec;
                public long timestampUsec;
                public string tag;
                public IList<string> categories;
                public string id; // added to allow mark as read/unread
                public string title;
                public int published;
                public int updated;
                FeedArticleListSelf canonical;
                FeedArticleListSelf alternate;
                public class FeedArticleItemSummary 
                {
                    public string direction;
                    public string content;
                }
                public FeedArticleItemSummary summary;
                public string author;
                public IList<string> likingusers;
                public IList<string> comments;
                public IList<string> annotations;
                public class FeedArticleOrigin
                {
                    public string streamid;
                    public string title;
                    public string htmlurl;
                }
                public FeedArticleOrigin origin;
            }
            public IList<FeedArticleItem> items;
        }

        private void SetItemContents(string output)
        {
            if (output != null)
            {
                try
                {
                    m_fal = JsonConvert.DeserializeObject<FeedArticleList>(output);
                    // Get the article ID to allow mark as read/mark as undread
                    // BOLDEST OF ASSUMPTIONS: The results come in the same order as the request
                    for (int i = 0; i < m_fal.items.Count(); i++)
                    {
                        m_fal.items[i].id = m_fil.itemrefs[i].id;
                    }
                    m_articleReady.Release();
                    if (ArticleDownloadCompleted != null) ArticleDownloadCompleted(this, EventArgs.Empty);
                }
                catch (Exception e)
                {
                    Console.WriteLine("FEED CONTENT: " + e.Message);
                    Console.WriteLine("FEED CONTENT: " + output);
                }
            }
            else
            {
                m_Error = "SetItemContents returned null";
            }
        }

        public void GetItemContents(IList<FeedItemElementList.FeedItemElement> articleList)
        {
            string sCommand = "";
            for (int i = 0; i < articleList.Count; i++)
            {
                if (sCommand.Length > 0) sCommand += "&";
                sCommand += "i=" + articleList[i].id;
            }
            AnthuanUtils.Post("https://theoldreader.com/reader/api/0/stream/items/contents?output=json", this.m_Token, sCommand, SetItemContents);
        }

        public void GetItemIdsAll(ItemListingType filter)
        {
            string sCommand;
            sCommand = "&s=user/-/state/com.google/reading-list";
            switch (filter)
            {
                case ItemListingType.ALL:
                    break;
                case ItemListingType.ONLYREAD:
                    sCommand = "s=user/-/state/com.google/read"; // it should be + instead of += on purpose!
                    break;
                case ItemListingType.ONLYUNREAD:
                    sCommand += "&xt=user/-/state/com.google/read";
                    break;
                default:
                    break;
            }
            AnthuanUtils.Get("https://theoldreader.com/reader/api/0/stream/items/ids?output=json" + sCommand, this.m_Token, SetItemIds);
        }

        public void GetItemIdsByFolder(string folderid, ItemListingType filter)
        {
            string sCommand;
            sCommand = "&s=" + folderid;
            switch (filter)
            {
                case ItemListingType.ALL:
                    break;
                case ItemListingType.ONLYREAD:
                    break;
                case ItemListingType.ONLYUNREAD:
                    sCommand += "&xt=user/-/state/com.google/read";
                    break;
                default:
                    break;
            }
            AnthuanUtils.Get("https://theoldreader.com/reader/api/0/stream/items/ids?output=json" + sCommand, this.m_Token, SetItemIds);
        }

        public void GetItemIdsByFeed(string feedid, ItemListingType filter)
        {
            string sCommand;
            sCommand = "&s=" + feedid;
            switch (filter)
            {
                case ItemListingType.ALL:
                    break;
                case ItemListingType.ONLYREAD:
                    break;
                case ItemListingType.ONLYUNREAD:
                    sCommand += "&xt=user/-/state/com.google/read";
                    break;
                default:
                    break;
            }
            AnthuanUtils.Get("https://theoldreader.com/reader/api/0/stream/items/ids?output=json" + sCommand, this.m_Token, SetItemIds);
        }
    }
}