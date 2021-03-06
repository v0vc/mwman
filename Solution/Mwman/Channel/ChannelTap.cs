﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Data.SQLite;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Windows;
using System.Windows.Media;
using HtmlAgilityPack;
using Mwman.Common;
using Mwman.Controls;
using Mwman.Models;
using Mwman.Video;

namespace Mwman.Channel
{
    public class ChannelTap : ChannelBase
    {
        public static string Typename = "Tapochek";

        private const string Hostbase = "tapochek.net";

        private const string Cookiename = "tapcookie.ck";

        private readonly MainWindowModel _model;

        private readonly BackgroundWorker _bgv = new BackgroundWorker();

        private CookieContainer _tapcookie;

        private ObservableCollection<VideoItemBase> _listSearchVideoItems;

        public ChannelTap(string login, string pass, string chanelname, string chanelowner, int ordernum, MainWindowModel model) : base(login, pass, chanelname, chanelowner, ordernum, model)
        {
            ChanelType = Typename;
            Cname = Cookiename;
            HostBase = Hostbase;
            InitialUrls();
            _model = model;
            LastColumnHeader = "Total DL";
            ViewSeedColumnHeader = "Seeders";
            DurationColumnHeader = "Size MB";
            TitleColumnHeader = "  Title   ";
            PlForumColumn = "F";
            _bgv.DoWork += _bgv_DoWork;
            _bgv.RunWorkerCompleted += _bgv_RunWorkerCompleted;
        }

        public ChannelTap(MainWindowModel model)
        {
            ChanelType = Typename;
            Cname = Cookiename;
            HostBase = Hostbase;
            InitialUrls(); 
            _model = model;
            LastColumnHeader = "Total DL";
            ViewSeedColumnHeader = "Seeders";
            DurationColumnHeader = "Size MB";
            TitleColumnHeader = "  Title   ";
            PlForumColumn = "F";
        }

        private void InitialUrls()
        {
            HostUrl = string.Format("http://{0}", HostBase);
            LoginUrl = string.Format("{0}/login.php", HostUrl);
            UserUrl = string.Format("{0}/tracker.php?rid", HostUrl);
            SearchUrl = string.Format("{0}/tracker.php?nm", HostUrl);
            TopicUrl = string.Format("{0}/viewtopic.php?t", HostUrl);
            IndexUrl = string.Format("{0}/index.php", HostUrl);
            Prefix = "tap";
        }

        private void _bgv_DoWork(object sender, DoWorkEventArgs e)
        {
            var type = e.Argument.ToString();
            e.Result = type;

            _tapcookie = ReadCookiesFromDiskBinary(Cname) ?? GetSession();

            string zap;

            switch (type)
            {
                case "Get":

                    #region Get

                    zap = string.Format("{0}={1}", UserUrl, ChanelOwner);

                    MakeTapResponse(zap, ListVideoItems, false);

                    #endregion

                    break;

                case "Popular":
                    break;

                case "Search":

                    #region Search

                    zap = string.Format("{0}={1}", SearchUrl, Searchkey);

                    MakeTapResponse(zap, _listSearchVideoItems, true);

                    #endregion

                    break;
            }
        }

        private void _bgv_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            if (e.Error != null)
            {
                _model.MySubscribe.Result = e.Error.Message;
                TimerCommon.Dispose();
                if (e.Error is SQLiteException)
                {
                    MessageBox.Show(e.Error.Message, "Database exception", MessageBoxButton.OK,
                        MessageBoxImage.Information);
                }
                else
                {
                    MessageBox.Show(e.Error.Message, "Common error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            else
            {
                if (e.Result == null)
                    return;

                switch (e.Result.ToString())
                {
                    case "Get":

                        var dir = Path.GetDirectoryName(Process.GetCurrentProcess().MainModule.FileName);
                        if (dir == null) return;
                        int totalrow;
                        Sqllite.CreateOrConnectDb(Subscribe.ChanelDb, ChanelOwner, out totalrow);
                        if (totalrow == 0)
                        {
                            foreach (VideoItemBase item in ListVideoItems)
                            {
                                if (Application.Current.Dispatcher.CheckAccess())
                                    item.IsHasFile = item.IsFileExist();
                                else
                                {
                                    VideoItemBase item1 = item;
                                    Application.Current.Dispatcher.Invoke(() => item1.IsHasFile = item1.IsFileExist());
                                }
                            }
                        }
                        else
                        {
                            foreach (VideoItemBase item in ListVideoItems)
                            {
                                if (Application.Current.Dispatcher.CheckAccess())
                                {
                                    item.IsHasFile = item.IsFileExist();
                                    item.IsSynced = Sqllite.IsTableHasRecord(Subscribe.ChanelDb, item.VideoID,
                                        ChanelOwner);
                                }
                                else
                                {
                                    VideoItemBase item1 = item;
                                    Application.Current.Dispatcher.Invoke(() =>
                                    {
                                        item1.IsHasFile = item1.IsFileExist();
                                        item1.IsSynced = Sqllite.IsTableHasRecord(Subscribe.ChanelDb, item1.VideoID,
                                            ChanelOwner);
                                    });
                                }
                            }
                            IsReady = !ListVideoItems.Select(x => x.IsSynced).Contains(false);
                            if (!IsReady)
                            {
                                NewItemCount = ListVideoItems.Count(x => x.IsSynced == false);
                            }
                        }

                        TimerCommon.Dispose();
                        _model.MySubscribe.Synctime = _model.MySubscribe.Synctime.Add(Synctime.Duration());

                        //Thread.Sleep(1000); //avoid too many connections

                        if (!Bgvdb.IsBusy)
                            Bgvdb.RunWorkerAsync(totalrow); //отдельный воркер для записи в базу

                        break;

                    case "Search":

                        TimerCommon.Dispose();
                        Subscribe.SetResult(string.Format("{0}: \"{1}\" searched in {2}", Typename, Searchkey,
                            Synctime.Duration().ToString(@"mm\:ss")));

                        break;
                }
            }
        }

        public override CookieContainer GetSession()
        {
            if (string.IsNullOrEmpty(Login) || string.IsNullOrEmpty(Password))
            {
                _model.MySubscribe.Result = "Ready";
                throw new Exception("Please, set Login and Password");
            }
            try
            {
                var cc = new CookieContainer();
                var req = (HttpWebRequest)WebRequest.Create(LoginUrl);
                req.CookieContainer = cc;
                req.Method = WebRequestMethods.Http.Post;
                req.Host = HostBase;
                req.KeepAlive = true;
                var postData = string.Format("login_username={0}&login_password={1}&login=%C2%F5%EE%E4", Uri.EscapeDataString(Login), Uri.EscapeDataString(Password));
                var data = Encoding.ASCII.GetBytes(postData);
                req.ContentLength = data.Length;
                req.Accept = "text/html,application/xhtml+xml,application/xml;q=0.9,*/*;q=0.8";
                req.UserAgent = "Mozilla/5.0 (Windows NT 6.3; WOW64; Trident/7.0; rv:11.0) like Gecko";
                req.ContentType = "application/x-www-form-urlencoded";
                req.Headers.Add("Cache-Control", "max-age=0");
                req.Headers.Add("Origin", HostUrl);
                req.Headers.Add("Accept-Language", "en-US,en;q=0.8");
                req.Headers.Add("Accept-Encoding", "gzip,deflate,sdch");
                req.Headers.Add("DNT", "1");
                req.Referer = IndexUrl;

                using (var stream = req.GetRequestStream())
                {
                    stream.Write(data, 0, data.Length);
                }

                var resp = (HttpWebResponse)req.GetResponse();
                if (resp.StatusCode == HttpStatusCode.OK)
                {
                    cc.Add(resp.Cookies);
                }
                WriteCookiesToDiskBinary(cc, Cname);
                return cc;
            }
            catch (Exception ex)
            {
                Subscribe.SetResult(ex.Message);
            }
            return null;
        }

        public override void GetItemsFromNet()
        {
            if (_bgv.IsBusy)
                return;
            if (string.IsNullOrEmpty(Login) || string.IsNullOrEmpty(Password))
            {
                Subscribe.SetResult("Please, set Login and Password");
                return;
            }

            InitializeTimer();

            if (IsFull)
            {
                if (Application.Current.Dispatcher.CheckAccess())
                {
                    ListVideoItems.Clear();
                    ListPlaylists.Clear();
                }
                else
                {
                    Application.Current.Dispatcher.Invoke(() => ListVideoItems.Clear());
                    Application.Current.Dispatcher.Invoke(() => ListPlaylists.Clear());
                }
            }
            _tapcookie = ReadCookiesFromDiskBinary(Cname) ?? GetSession();
            _bgv.RunWorkerAsync("Get");
            
        }

        public override void AutorizeChanel()
        {
            _tapcookie = GetSession();
        }

        public override void DownloadItem(IList list, bool isAudio)
        {
            _tapcookie = ReadCookiesFromDiskBinary(Cname) ?? GetSession();
            
            foreach (VideoItemBase item in list)
            {
                DownloadItem(item, false);
            }
        }

        public override void DownloadItem(VideoItemBase item, bool isGetCookie)
        {
            ChanelColor = new SolidColorBrush(Color.FromRgb(152, 251, 152));

            if (isGetCookie)
                _tapcookie = ReadCookiesFromDiskBinary(Cname) ?? GetSession();

            // Construct HTTP request to get the file
            var httpRequest = (HttpWebRequest) WebRequest.Create(item.VideoLink);
            httpRequest.Method = WebRequestMethods.Http.Get;

            httpRequest.Referer = string.Format("{0}={1}", TopicUrl, item.VideoID);
            httpRequest.CookieContainer = _tapcookie;

            var httpResponse = (HttpWebResponse) httpRequest.GetResponse();
            if (httpResponse.ContentType.Contains("torrent"))
            {
                Stream httpResponseStream = httpResponse.GetResponseStream();

                //string fileName = Path.GetFileName(httpResponse.Headers["filename"]);

                const int bufferSize = 1024;
                var buffer = new byte[bufferSize];

                // Read from response and write to file
                var ddir =
                    new DirectoryInfo(Path.Combine(Subscribe.DownloadPath,
                        string.Format("{0}-{1}({2})", Prefix, item.VideoOwnerName, item.VideoOwner)));
                if (!ddir.Exists)
                    ddir.Create();

                ddir.LastWriteTime = DateTime.Now;

                var tap = item as VideoItemTap;
                if (tap != null)
                {
                    var dpath =
                        VideoItemBase.AviodTooLongFileName(Path.Combine(ddir.FullName, tap.MakeTorrentFileName(false)));
                    FileStream fileStream = File.Create(dpath);
                    int bytesRead;
                    while (httpResponseStream != null &&
                           (bytesRead = httpResponseStream.Read(buffer, 0, bufferSize)) != 0)
                    {
                        fileStream.Write(buffer, 0, bytesRead);
                    } // end while
                    var fn = new FileInfo(dpath);
                    if (fn.Exists)
                        tap.FilePath = fn.FullName;

                    tap.IsHasFile = fn.Exists;
                    tap.FileType = "torrent";
                }
                _model.MySubscribe.Result = "Download OK: " + item.Title.Trim();
            }
            else
            {
                DownloadItem(item, true);
            }
        }

        public override void SearchItems(string key, ObservableCollection<VideoItemBase> listSearchVideoItems)
        {
            InitializeTimer();
            _listSearchVideoItems = listSearchVideoItems;
            _model.MySubscribe.ResCount = _listSearchVideoItems.Count;
            Searchkey = key;
            if (!_bgv.IsBusy)
                _bgv.RunWorkerAsync("Search");
        }

        public override void GetPopularItems(string key, ObservableCollection<VideoItemBase> listPopularVideoItems, string mode)
        {
            throw new NotImplementedException();
        }

        public override void CancelDownloading()
        {
            throw new NotImplementedException();
        }

        public override void UpdatePlaylist()
        {
            throw new NotImplementedException();
        }

        private void MakeTapResponse(string zap, ObservableCollection<VideoItemBase> listVideoItems, bool isSearch)
        {
            listVideoItems.CollectionChanged += listVideoItems_CollectionChanged;
            HtmlDocument doc;
            var results = GetAllLinks(_tapcookie, zap, out doc);
            if (!results.Any())
            {
                _tapcookie = GetSession();
                results = GetAllLinks(_tapcookie, zap, out doc);
            }
            foreach (HtmlNode node in results)
            {
                var v = new VideoItemTap(node, Prefix)
                {
                    Num = listVideoItems.Count + 1, ParentChanel = this
                };

                var pl = new Playlist(v.PlaylistTitle, v.PlaylistID, v.PlaylistID);
                if (!ListPlaylists.Select(x => x.Title).Contains(pl.Title))
                {
                    Application.Current.Dispatcher.Invoke(() => ListPlaylists.Add(pl));
                }

                if (IsFull)
                {
                    if (listVideoItems.Contains(v) || string.IsNullOrEmpty(v.Title))
                        continue;
                    if (Application.Current.Dispatcher.CheckAccess())
                        listVideoItems.Add(v);
                    else
                        Application.Current.Dispatcher.Invoke(() => listVideoItems.Add(v));
                }
                else
                {
                    if (listVideoItems.Select(x => x.VideoID).Contains(v.VideoID) ||
                        string.IsNullOrEmpty(v.Title))
                        continue;
                    if (Application.Current.Dispatcher.CheckAccess())
                        listVideoItems.Insert(0, v);
                    else
                        Application.Current.Dispatcher.Invoke(() => listVideoItems.Insert(0, v));
                }
            }

            if (!IsFull)
            {
                for (int i = 0; i < listVideoItems.Count; i++)
                {
                    var k = i;
                    listVideoItems[i].Num = k + 1;
                }
                return;
            }

            var serchlinkss = isSearch ? GetAllSearchLinks(doc) : GetAllSearchLinks(doc, ChanelOwner);
            Thread.Sleep(500);
            foreach (string link in serchlinkss)
            {
                results = GetAllLinks(_tapcookie, link, out doc);
                foreach (HtmlNode nodes in results)
                {
                    var v = new VideoItemTap(nodes, Prefix);
                    var pl = new Playlist(v.PlaylistTitle, v.PlaylistID, v.PlaylistID);
                    if (!ListPlaylists.Select(x => x.Title).Contains(pl.Title))
                    {
                        Application.Current.Dispatcher.Invoke(() => ListPlaylists.Add(pl));
                    }
                    if (!listVideoItems.Contains(v) && !listVideoItems.Select(x => x.Title).Contains(v.Title) && !string.IsNullOrEmpty(v.Title))
                    {
                        v.Num = listVideoItems.Count + 1;
                        v.ParentChanel = this;
                        if (Application.Current.Dispatcher.CheckAccess())
                            listVideoItems.Add(v);
                        else
                            Application.Current.Dispatcher.Invoke(() => listVideoItems.Add(v));
                    }
                }
                Thread.Sleep(500);
            }
            listVideoItems.CollectionChanged -= listVideoItems_CollectionChanged;
        }

        private IEnumerable<string> GetAllSearchLinks(HtmlDocument doc, string pid)
        {
            var hrefTags = new List<string>();

            var block = doc.DocumentNode.Descendants("div").Where(d => d.Attributes.Contains("class") && d.Attributes["class"].Value.Equals("nav")).ToList();

            if (block.Count==2)
            {
                var hr = block[1].Descendants("a");
                foreach (HtmlNode link in hr)
                {
                    HtmlAttribute att = link.Attributes["href"];
                    if (att.Value != null && !hrefTags.Contains(att.Value) && att.Value.StartsWith("tracker"))
                        hrefTags.Add(att.Value);
                }
            }
            return hrefTags.Select(link => string.Format("{0}/{1}", HostUrl, link)).ToList();
        }

        private IEnumerable<string> GetAllSearchLinks(HtmlDocument doc)
        {
            var hrefTags = new List<string>();

            var counts = doc.DocumentNode.Descendants("a").Where(d => d.Attributes.Contains("class") && d.Attributes["class"].Value.Equals("pg"));

            foreach (HtmlNode link in counts)
            {
                HtmlAttribute att = link.Attributes["href"];
                if (att.Value != null && !hrefTags.Contains(att.Value))
                {
                    var sp = att.Value.Split(';');
                    if (sp.Length == 2)
                    {
                        var res = string.Format("{0}{1}&nm={2}", sp[0].Remove(sp[0].Length - 3), sp[1], Searchkey);
                        if (!hrefTags.Contains(res))
                            hrefTags.Add(res);
                    }
                }
            }

            return hrefTags.Select(link => string.Format("{0}/{1}", HostUrl, link)).ToList();
        }

        private static List<HtmlNode> GetAllLinks(CookieContainer cookie, string zap, out HtmlDocument doc)
        {
            var wc = new WebClientEx(cookie);
            var res = wc.DownloadString(zap);
            doc = new HtmlDocument();
            doc.LoadHtml(res);
            return doc.DocumentNode.Descendants("tr")
                .Where(
                    d =>
                        d.Attributes.Contains("class") &&
                        d.Attributes["class"].Value.Equals("tCenter")).ToList();
        }

        private void listVideoItems_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            var collection = sender as ObservableCollection<VideoItemBase>;
            if (collection != null)
                _model.MySubscribe.ResCount = collection.Count;
        }

        private void InitializeTimer()
        {
            Synctime = new TimeSpan();
            var tcb = new TimerCallback(tmr_Tick);
            TimerCommon = new Timer(tcb, null, 0, 1000);
        }

        private void tmr_Tick(object o)
        {
            _model.MySubscribe.Result = "Working...";
            Synctime = Synctime.Add(TimeSpan.FromSeconds(1));
        }
    }
}
