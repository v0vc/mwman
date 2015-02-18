using System;
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
using Mwman.Common;
using Mwman.Controls;
using Mwman.Models;
using Mwman.Video;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Mwman.Channel
{
    public class ChannelYou : ChannelBase
    {
        public static string Typename = "YouTube";

        private int _step;

        private readonly List<VideoItemBase> _selectedListVideoItemsList = new List<VideoItemBase>();

        private ObservableCollection<VideoItemBase> _listSearchVideoItems;

        private ObservableCollection<VideoItemBase> _listPopularVideoItems;

        private string _searchkey;

        private string _cul;

        private readonly MainWindowModel _model;

        private readonly BackgroundWorker _bgv = new BackgroundWorker();

        public int MinRes { get; set; }
        public int MaxResults { get; set; }

        public ChannelYou(string login, string pass, string chanelname, string chanelowner, int ordernum, MainWindowModel model)
            : base(login, pass, chanelname, chanelowner, ordernum, model)
        {
            ChanelType = Typename;
            _model = model;
            MinRes = 1;
            MaxResults = 25;
            LastColumnHeader = "Download";
            ViewSeedColumnHeader = "Views";
            DurationColumnHeader = "Duration";
            TitleColumnHeader = "Playlist:";
            _bgv.DoWork += _bgv_DoWork;
            _bgv.RunWorkerCompleted += _bgv_RunWorkerCompleted;
        }

        public ChannelYou(MainWindowModel model)
        {
            ChanelType = Typename;
            _model = model;
            LastColumnHeader = "Download";
            ViewSeedColumnHeader = "Views";
            DurationColumnHeader = "Duration";
            TitleColumnHeader = "Playlist:";
            _bgv.WorkerSupportsCancellation = true;
            _bgv.DoWork += _bgv_DoWork;
            _bgv.RunWorkerCompleted += _bgv_RunWorkerCompleted;
        }

        public ChannelYou(JToken pair)
        {
            ChanelType = Typename;
            MinRes = 1;
            MaxResults = 25;
            LastColumnHeader = "Download";
            ViewSeedColumnHeader = "Views";
            DurationColumnHeader = "Duration";
            ChanelName = pair["title"]["$t"].ToString();
            var owner = pair["author"][0]["uri"]["$t"].ToString().Split('/');
            ChanelOwner = owner[owner.Length - 1];

        }

        private void _bgv_DoWork(object sender, DoWorkEventArgs e)
        {
            var type = e.Argument.ToString();
            e.Result = type;

            string zap;

            switch (type)
            {
                case "Get":

                    #region Get

                    while (true)
                    {
                        var wc = new WebClient {Encoding = Encoding.UTF8};
                        zap =
                            string.Format(
                                "https://gdata.youtube.com/feeds/api/users/{0}/uploads?alt=json&start-index={1}&max-results={2}",
                                ChanelOwner, MinRes, MaxResults);
                        var res = wc.DownloadString(zap);
                        var jsvideo = (JObject) JsonConvert.DeserializeObject(res);
                        if (jsvideo == null)
                            return;
                        int total;
                        if (int.TryParse(jsvideo["feed"]["openSearch$totalResults"]["$t"].ToString(), out total))
                        {
                            if (total != 0)
                            {
                                foreach (JToken pair in jsvideo["feed"]["entry"])
                                {
                                    var v = new VideoItemYou(pair, false)
                                    {
                                        Num = ListVideoItems.Count + 1,
                                        VideoOwner = ChanelOwner,
                                        ParentChanel = this
                                    };

                                    if (IsFull)
                                    {
                                        if (ListVideoItems.Contains(v) || string.IsNullOrEmpty(v.Title))
                                            continue;
                                        if (Application.Current.Dispatcher.CheckAccess())
                                            ListVideoItems.Add(v);
                                        else
                                            Application.Current.Dispatcher.Invoke(() => ListVideoItems.Add(v));
                                    }
                                    else
                                    {
                                        if (ListVideoItems.Select(x => x.VideoID).Contains(v.VideoID) ||
                                            string.IsNullOrEmpty(v.Title))
                                            continue;
                                        if (Application.Current.Dispatcher.CheckAccess())
                                            ListVideoItems.Insert(0, v);
                                        else
                                            Application.Current.Dispatcher.Invoke(() => ListVideoItems.Insert(0, v));
                                    }
                                }
                            }
                            if (!IsFull)
                            {
                                for (int i = 0; i < ListVideoItems.Count; i++)
                                {
                                    var k = i;
                                    ListVideoItems[i].Num = k + 1;
                                }
                                return;
                            }

                            if (total > ListVideoItems.Count)
                            {
                                MinRes = MinRes + MaxResults;
                                if (MinRes < total)
                                    continue;
                            }

                            MinRes = 1;
                        }
                        break;
                    }

                    if (IsFull)
                    {
                        UpdatePlayListBgv();
                    }

                    #endregion

                    break;

                case "Popular":

                    zap =
                        string.Format(
                            "https://gdata.youtube.com/feeds/api/standardfeeds/{0}/most_popular?time=today&v=2&alt=json",
                            _cul);

                    MakeYouResponse(zap, _listPopularVideoItems);

                    FillMostPopularChanels();

                    break;

                case "Search":

                    zap = string.Format("https://gdata.youtube.com/feeds/api/videos?q={0}&max-results=50&v=2&alt=json",
                        _searchkey);

                    MakeYouResponse(zap, _listSearchVideoItems);

                    break;

                case "PopFill":

                    #region PopFill

                    Application.Current.Dispatcher.Invoke(() => ListPopularVideoItems.Clear());

                    while (true)
                    {
                        var wc = new WebClient {Encoding = Encoding.UTF8};
                        zap =
                            string.Format(
                                "https://gdata.youtube.com/feeds/api/users/{0}/uploads?alt=json&start-index={1}&max-results={2}",
                                CurrentPopularChannel.ChanelOwner, MinRes, MaxResults);
                        var res = wc.DownloadString(zap);
                        var jsvideo = (JObject) JsonConvert.DeserializeObject(res);
                        if (jsvideo == null)
                            return;
                        int total;
                        if (int.TryParse(jsvideo["feed"]["openSearch$totalResults"]["$t"].ToString(), out total))
                        {
                            if (total != 0)
                            {
                                foreach (JToken pair in jsvideo["feed"]["entry"])
                                {
                                    Model.MySubscribe.ResCount = ListPopularVideoItems.Count;
                                    var v = new VideoItemYou(pair, false)
                                    {
                                        Num = ListPopularVideoItems.Count + 1,
                                        VideoOwner = ChanelOwner,
                                        ParentChanel = this,
                                        Region = CurrentPopularChannel.ChanelName
                                    };
                                    if (IsFull)
                                    {
                                        if (ListPopularVideoItems.Contains(v) || string.IsNullOrEmpty(v.Title))
                                            continue;
                                        if (Application.Current.Dispatcher.CheckAccess())
                                            ListPopularVideoItems.Add(v);
                                        else
                                            Application.Current.Dispatcher.Invoke(() => ListPopularVideoItems.Add(v));
                                    }
                                    else
                                    {
                                        if (ListPopularVideoItems.Select(x => x.VideoID).Contains(v.VideoID) ||
                                            string.IsNullOrEmpty(v.Title))
                                            continue;
                                        if (Application.Current.Dispatcher.CheckAccess())
                                            ListPopularVideoItems.Insert(0, v);
                                        else
                                            Application.Current.Dispatcher.Invoke(
                                                () => ListPopularVideoItems.Insert(0, v));
                                    }
                                }
                            }
                            if (!IsFull)
                            {
                                for (int i = 0; i < ListPopularVideoItems.Count; i++)
                                {
                                    var k = i;
                                    ListPopularVideoItems[i].Num = k + 1;
                                }
                                return;
                            }

                            if (total > ListPopularVideoItems.Count)
                            {
                                MinRes = MinRes + MaxResults;
                                if (MinRes < total)
                                    continue;
                            }
                            MinRes = 1;
                        }
                        break;
                    }

                    #endregion

                    break;

                case "Playlist":

                    UpdatePlayListBgv();

                    break;
            }
        }

        private void UpdatePlayListBgv()
        {
            MinRes = 1;
            string zap;

            while (true)
            {
                var wc = new WebClient { Encoding = Encoding.UTF8 };

                zap = string.Format(
                    "http://gdata.youtube.com/feeds/api/users/{0}/playlists?v=2&alt=json&start-index={1}&max-results={2}",
                    ChanelOwner, MinRes, MaxResults);
                var res = wc.DownloadString(zap);
                var jsvideo = (JObject)JsonConvert.DeserializeObject(res);
                if (jsvideo == null)
                    return;
                int total;
                if (int.TryParse(jsvideo["feed"]["openSearch$totalResults"]["$t"].ToString(), out total))
                {
                    if (total != 0)
                    {
                        foreach (JToken pair in jsvideo["feed"]["entry"])
                        {
                            var title = pair["title"]["$t"].ToString();
                            var id = pair["yt$playlistId"]["$t"].ToString();
                            var link = string.Format("{0}&alt=json", pair["content"]["src"]);
                            var pl = new Playlist(title, id, link);

                            if (Application.Current.Dispatcher.CheckAccess())
                                ListPlaylists.Add(pl);
                            else
                                Application.Current.Dispatcher.Invoke(() => ListPlaylists.Add(pl));
                        }
                    }
                }

                if (total > ListVideoItems.Count)
                {
                    MinRes = MinRes + MaxResults;
                    if (MinRes < total)
                        continue;
                }

                MinRes = 1;
                break;
            }

            foreach (Playlist pl in ListPlaylists.Where(x => !string.IsNullOrEmpty(x.ContentLink)))
            {
                while (true)
                {
                    var wc = new WebClient { Encoding = Encoding.UTF8 };

                    zap = string.Format("{0}&start-index={1}&max-results={2}", pl.ContentLink, MinRes,
                        MaxResults);
                    var res = wc.DownloadString(zap);
                    var jsvideo = (JObject)JsonConvert.DeserializeObject(res);
                    if (jsvideo == null)
                        return;
                    int total;
                    if (int.TryParse(jsvideo["feed"]["openSearch$totalResults"]["$t"].ToString(), out total))
                    {
                        if (total != 0)
                        {
                            foreach (JToken pair in jsvideo["feed"]["entry"])
                            {
                                var vi = new VideoItemYou(pair, pl.ListID, pl.Title);

                                var item = ListVideoItems.FirstOrDefault(x => x.VideoID == vi.VideoID);
                                if (item != null)
                                {
                                    item.PlaylistID = vi.PlaylistID;
                                    item.PlaylistTitle = vi.PlaylistTitle;
                                }
                            }
                        }
                    }

                    if (total > ListVideoItems.Count)
                    {
                        MinRes = MinRes + MaxResults;
                        if (MinRes < total)
                            continue;
                    }
                    MinRes = 1;
                    break;
                }
            }

            Application.Current.Dispatcher.Invoke(
                () => ListPlaylists.Add(new Playlist("ALL", "ALL", string.Empty)));

        }

        private void _bgv_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            MinRes = 1;
            if (e.Error != null)
            {
                _model.MySubscribe.Result = e.Error.Message;
                TimerCommon.Dispose();
                if (e.Error is SQLiteException)
                {
                    MessageBox.Show(e.Error.Message, "Database exception", MessageBoxButton.OK, MessageBoxImage.Information);
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

                        #region Get

                        var dir = Path.GetDirectoryName(Process.GetCurrentProcess().MainModule.FileName);
                        if (dir == null) return;
                        int totalrow;
                        Sqllite.CreateOrConnectDb(Subscribe.ChanelDb, ChanelOwner, out totalrow);
                        if (totalrow == 0)
                        {
                            foreach (VideoItemBase item in ListVideoItems)
                            {
                                if (Application.Current.Dispatcher.CheckAccess())
                                {
                                    item.IsHasFile = item.IsFileExist();
                                }
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

                        if (!Bgvdb.IsBusy)
                            Bgvdb.RunWorkerAsync(totalrow); //отдельный воркер для записи в базу

                        #endregion

                        break;

                    case "Popular":

                        TimerCommon.Dispose();
                        Subscribe.SetResult(string.Format("\'{0}\' synced in {1}", _model.SelectedCountry.Key,
                            Synctime.Duration().ToString(@"mm\:ss")));

                        break;

                    case "Search":

                        TimerCommon.Dispose();
                        Subscribe.SetResult(string.Format("{0}: \'{1}\' searched in {2}", Typename, _searchkey,
                            Synctime.Duration().ToString(@"mm\:ss")));

                        break;

                    case "PopFill":

                        TimerCommon.Dispose();
                        Subscribe.SetResult(string.Format("{0}: \'{1}\' get in {2}", Typename, CurrentPopularChannel.ChanelName,
                            Synctime.Duration().ToString(@"mm\:ss")));
                        break;

                    case "Playlist":

                        foreach (VideoItemBase item in ListVideoItems)
                        {
                            Sqllite.UpdateValue(Subscribe.ChanelDb, Sqllite.PId, item.PlaylistID, Sqllite.Id, item.VideoID);
                            Sqllite.UpdateValue(Subscribe.ChanelDb, Sqllite.PTitle, item.PlaylistTitle, Sqllite.Id, item.VideoID);
                        }
                        TimerCommon.Dispose();
                        Subscribe.SetResult(string.Format("{0}: \'{1}\' playlists updated in {2}", Typename, ChanelName,
                            Synctime.Duration().ToString(@"mm\:ss")));

                        break;
                }
            }
        }

        private void FillMostPopularChanels()
        {
            MinRes = 1;
            while (true)
            {
                var wc = new WebClient { Encoding = Encoding.UTF8 };
                var zap =
                    string.Format(
                        "https://gdata.youtube.com/feeds/api/channelstandardfeeds/{0}/most_viewed?v=2&alt=json&start-index={1}&max-results={2}",
                        _cul, MinRes, MaxResults);
                var res = wc.DownloadString(zap);
                var jsvideo = (JObject)JsonConvert.DeserializeObject(res);
                if (jsvideo == null)
                    return;
                int total;
                if (int.TryParse(jsvideo["feed"]["openSearch$totalResults"]["$t"].ToString(), out total))
                {
                    if (total != 0)
                    {
                        foreach (JToken pair in jsvideo["feed"]["entry"])
                        {
                            var channel = new ChannelYou(pair);
                            if (Application.Current.Dispatcher.CheckAccess())
                                ListPopularChannels.Add(channel);
                            else
                                Application.Current.Dispatcher.Invoke(() => ListPopularChannels.Add(channel));

                        }
                    }

                }
                if (total > ListPopularChannels.Count)
                {
                    MinRes = MinRes + MaxResults;
                    if (MinRes < total)
                        continue;
                }
                break;
            }
        }

        private void MakeYouResponse(string zap, ObservableCollection<VideoItemBase> listVideoItems)
        {
            listVideoItems.CollectionChanged += listVideoItems_CollectionChanged;

            var wc = new WebClient { Encoding = Encoding.UTF8 };
            
            var res = wc.DownloadString(zap);
            var jsvideo = (JObject)JsonConvert.DeserializeObject(res);
            if (jsvideo == null)
                return;
            int total;
            if (int.TryParse(jsvideo["feed"]["openSearch$totalResults"]["$t"].ToString(), out total))
            {
                foreach (JToken pair in jsvideo["feed"]["entry"])
                {
                    var v = new VideoItemYou(pair, true)
                    {
                        Num = listVideoItems.Count + 1,
                        ParentChanel = this,
                    };
                    v.Region = v.VideoOwner;

                    if (Application.Current.Dispatcher.CheckAccess())
                        AddItems(v, listVideoItems);
                    else
                        Application.Current.Dispatcher.Invoke(() => AddItems(v, listVideoItems));
                }
            }

            Model.MySubscribe.ResCount = listVideoItems.Count;
            listVideoItems.CollectionChanged -= listVideoItems_CollectionChanged;
        }

        private void listVideoItems_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            var collection = sender as ObservableCollection<VideoItemBase>;
            if (collection != null)
                _model.MySubscribe.ResCount = collection.Count;
        }

        public override void GetItemsFromNet()
        {
            if (_bgv.IsBusy)
                return;
            Subscribe.SetResult("Working...");

            InitializeTimer();

            if (IsFull)
                Application.Current.Dispatcher.Invoke(() => ListVideoItems.Clear());
            _bgv.RunWorkerAsync("Get");
        }

        public override void DownloadItem(IList list, bool isAudio)
        {
            if (string.IsNullOrEmpty(Subscribe.YoudlPath))
            {
                MessageBox.Show("Please set path to Youtube-dl in the Settings", "Info", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            ChanelColor = new SolidColorBrush(Color.FromRgb(152, 251, 152));

            if (Subscribe.IsAsyncDl)
                GetVideosASync(list, isAudio);
            else
                GetVideosSync();
        }

        public override void DownloadItem(VideoItemBase item, bool isGetCookie)
        {
            throw new NotImplementedException();
        }

        public override void SearchItems(string key, ObservableCollection<VideoItemBase> listSearchVideoItems)
        {
            InitializeTimer();
            _listSearchVideoItems = listSearchVideoItems;
            _model.MySubscribe.ResCount = _listSearchVideoItems.Count;
            _searchkey = key;
            if (!_bgv.IsBusy)
                _bgv.RunWorkerAsync("Search");
        }

        public override void GetPopularItems(string key, ObservableCollection<VideoItemBase> listPopularVideoItems, string mode)
        {
            InitializeTimer();
            _cul = key;
            _listPopularVideoItems = listPopularVideoItems;
            _model.MySubscribe.ResCount = _listPopularVideoItems.Count;
            if (!_bgv.IsBusy)
                _bgv.RunWorkerAsync(mode);
        }

        public override void CancelDownloading()
        {
            foreach (VideoItemYou item in SelectedListVideoItems.Cast<VideoItemYou>().Where(item => item.IsDownLoading))
            {
                item.CancelDownloading();
            }
        }

        public override CookieContainer GetSession()
        {
            return null;
        }

        public override void AutorizeChanel()
        {

        }

        public void UpdatePlaylist()
        {
            if (_bgv.IsBusy)
                return;
            Subscribe.SetResult("Working...");

            InitializeTimer();

            Application.Current.Dispatcher.Invoke(() => ListPlaylists.Clear());

            _bgv.RunWorkerAsync("Playlist");
        }

        private static void AddItems(VideoItemBase v, ICollection<VideoItemBase> listPopularVideoItems)
        {
            listPopularVideoItems.Add(v);
            v.IsHasFile = v.IsFileExist();
            v.IsSynced = true;
        }

        public void DownloadVideoInternal(IList list)
        {
            foreach (VideoItemYou item in list)
            {
                item.DownloadInternal();
            }
        }

        private static void GetVideosASync(IEnumerable list, bool isAudio)
        {
            foreach (VideoItemBase item in list)
            {
                item.DownloadItem(isAudio);
            }
        }

        private void GetVideosSync()
        {
            _selectedListVideoItemsList.Clear();
            foreach (VideoItemBase item in SelectedListVideoItems)
            {
                _selectedListVideoItemsList.Add(item);
            }

            GetVideos();
        }

        private void GetVideos()
        {
            if (SelectedListVideoItems.Count == 1)
                _step = 0;

            var item = _selectedListVideoItemsList[_step] as VideoItemYou;
            if (item != null)
            {
                item.Activate += youwr_nextstep;
                item.DownloadItem(false);
                _step++;
            }
        }

        private void youwr_nextstep()
        {
            if (_step < _selectedListVideoItemsList.Count)
                GetVideos();
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

        //public void GetChanelVideoItemsWithoutGoogle()
        //{
        //    ListVideoItems.Clear();
        //    var web = new HtmlWeb
        //    {
        //        AutoDetectEncoding = false,
        //        OverrideEncoding = Encoding.UTF8,
        //    };
        //    var chanelDoc = web.Load(ChanelLink.AbsoluteUri);
        //    if (chanelDoc == null)
        //        throw new HtmlWebException("Can't load page: " + Environment.NewLine + ChanelLink.AbsoluteUri);
        //    //var i = 0;
        //    foreach (HtmlNode link in chanelDoc.DocumentNode.SelectNodes("//a[@href]"))
        //    {
        //        var att = link.Attributes["href"];
        //        string parsed;
        //        if (!IsLinkCorrectYouTube(att.Value, out parsed))
        //            continue;
        //        var parsedtrim = parsed.TrimEnd('&');
        //        var sp = parsedtrim.Split('=');
        //        if (sp.Length == 2 && sp[1].Length == 11)
        //        {
        //            var v = new VideoItem(parsedtrim, sp[1]);
        //            //var removedBreaksname = link.InnerText.Trim().Replace("\r\n", string.Empty).Replace("\n", string.Empty).Replace("\r", string.Empty);
        //            //v.VideoName = removedBreaksname;
        //            if (!ListVideoItems.Select(x => x.RawUrl).ToList().Contains(v.RawUrl))
        //            {
        //                ListVideoItems.Add(v);
        //                //i++;
        //            }
        //        }
        //    }
        //}

        //private static bool IsLinkCorrectYouTube(string input, out string parsedres)
        //{
        //    var res = false;
        //    parsedres = string.Empty;
        //    var regExp = new Regex(@"(watch\?.)(.+?)(?:[^-a-zA-Z0-9]|$)");
        //    //var regExp = new Regex(@"(?:youtu\.be\/|youtube.com\/(?:watch\?.*\bv=|embed\/|v\/)|ytimg\.com\/vi\/)(.+?)(?:[^-a-zA-Z0-9]|$)");
        //    //var regExp = new Regex(@"/^.*((youtu.be\/)|(v\/)|(\/u\/\w\/)|(embed\/)|(watch\?))\??v?=?([^#\&\?]*).*/");
        //    //var regExp = new Regex(@"/^.*(youtu.be\/|v\/|u\/\w\/|embed\/|watch\?v=|\&v=)([^#\&\?]*).*/");
        //    //var regExp = new Regex(@"/(?:https?://)?(?:www\.)?youtu(?:be\.com/watch\?(?:.*?&(?:amp;)?)?v=|\.be/)([\w‌​\-]+)(?:&(?:amp;)?[\w\?=]*)?/");
        //    //var regExp = new Regex(@"http://(?:www\.)?youtu(?:be\.com/watch\?v=|\.be/)(\w*)(&(amp;)?[\w\?=]*)?");
        //    //var regExp = new Regex(@"(?:(?:watch\?.*\bv=|embed\/|v\/)|ytimg\.com\/vi\/)(.+?)(?:[^-a-zA-Z0-9]|$)");
        //    var match = regExp.Match(input);
        //    if (match.Success)
        //    {
        //        parsedres = match.Value;
        //        res = true;
        //    }
        //    return res;
        //}
    }
}
