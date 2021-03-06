﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Data.Common;
using System.IO;
using System.Linq;
using System.Net;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Threading;
using System.Windows;
using System.Windows.Media;
using Mwman.Common;
using Mwman.Controls;
using Mwman.Models;
using Mwman.Video;

namespace Mwman.Channel
{
    public abstract class ChannelBase : INotifyPropertyChanged
    {
        #region INotifyPropertyChanged
        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChangedEventHandler handler = PropertyChanged;
            if (handler != null) handler(this, new PropertyChangedEventArgs(propertyName));
        } 
        #endregion

        #region Fields

        private Brush _chanelColor;

        private string _password;

        private string _login;

        private bool _isReady;

        private bool _isFavorite;

        private string _chanelName;

        private IList _selectedListVideoItems = new ArrayList();

        private VideoItemBase _currentVideoItem;

        private string _titleFilter;

        private readonly List<VideoItemBase> _filterlist = new List<VideoItemBase>();

        public readonly BackgroundWorker Bgvdb = new BackgroundWorker();

        private string _lastColumnHeader;

        private string _viewSeedColumnHeader;

        private string _durationColumnHeader;

        private string _titleColumnHeader;

        private string _plForumColumn;

        private int _newitemcount;

        private Playlist _currentPlaylist;

        private ChannelBase _currentPopularChannel;

        #endregion

        #region Properties
        public MainWindowModel Model { get; set; }
        public TimeSpan Synctime { get; set; }

        public Timer TimerCommon;

        public Brush ChanelColor
        {
            get { return _chanelColor; }
            set
            {
                _chanelColor = value;
                OnPropertyChanged();
            }
        }

        public string Login
        {
            get { return _login; }
            set
            {
                _login = value;
                OnPropertyChanged();
            }
        }

        public string Password
        {
            get { return _password; }
            set
            {
                _password = value;
                OnPropertyChanged();
            }
        }

        public string ChanelType { get; set; }

        public string ChanelOwner { get; set; }

        public string ChanelName
        {
            get { return _chanelName; }
            set
            {
                _chanelName = value;
                OnPropertyChanged();
            }
        }

        public string LastColumnHeader
        {
            get { return _lastColumnHeader; }
            set
            {
                _lastColumnHeader = value;
                OnPropertyChanged();
            }
        }

        public string ViewSeedColumnHeader
        {
            get { return _viewSeedColumnHeader; }
            set
            {
                _viewSeedColumnHeader = value;
                OnPropertyChanged();
            }
        }

        public string DurationColumnHeader
        {
            get { return _durationColumnHeader; }
            set
            {
                _durationColumnHeader = value;
                OnPropertyChanged();
            }
        }

        public string TitleColumnHeader
        {
            get { return _titleColumnHeader; }
            set
            {
                _titleColumnHeader = value;
                OnPropertyChanged();
            }
        }

        public string PlForumColumn
        {
            get { return _plForumColumn; }
            set
            {
                _plForumColumn = value;
                OnPropertyChanged();
            }
        }

        public int OrderNum { get; set; }

        public bool IsReady
        {
            get { return _isReady; }
            set
            {
                _isReady = value;
                OnPropertyChanged();
            }
        }

        public bool IsFull { get; set; }

        public string Cname { get; set; }

        public string Prefix { get; set; }

        public string Searchkey { get; set; }

        public string LoginUrl { get; set; }

        public string UserUrl { get; set; }

        public string SearchUrl { get; set; }

        public string TopicUrl { get; set; }

        public string IndexUrl { get; set; }

        public string HostUrl { get; set; }

        public string HostBase { get; set; }

        public int NewItemCount
        {
            get { return _newitemcount; }
            set
            {
                _newitemcount = value;
                OnPropertyChanged();
            }
        }

        public ObservableCollection<VideoItemBase> ListVideoItems { get; set; }

        public ObservableCollection<VideoItemBase> ListPopularVideoItems { get; set; }

        public ObservableCollection<VideoItemBase> ListSearchVideoItems { get; set; }

        public ObservableCollection<Playlist> ListPlaylists { get; set; }

        public ObservableCollection<ChannelBase> ListPopularChannels { get; set; }

        public Playlist CurrentPlaylist
        {
            get { return _currentPlaylist; }
            set
            {
                _currentPlaylist = value;
                OnPropertyChanged();
                Filter();
            }
        }

        public ChannelBase CurrentPopularChannel
        {
            get { return _currentPopularChannel; }
            set
            {
                _currentPopularChannel = value;
                OnPropertyChanged();
                GetPopularChannelItems();
            }
        }

        public IList SelectedListVideoItems
        {
            get { return _selectedListVideoItems; }
            set
            {
                _selectedListVideoItems = value;
                OnPropertyChanged();
            }
        }

        public VideoItemBase CurrentVideoItem
        {
            get { return _currentVideoItem; }
            set
            {
                _currentVideoItem = value;
                OnPropertyChanged();
            }
        }

        public bool IsFavorite
        {
            get { return _isFavorite; }
            set
            {
                _isFavorite = value;
                OnPropertyChanged();
            }
        }

        public string TitleFilter
        {
            get { return _titleFilter; }
            set
            {
                _titleFilter = value;
                OnPropertyChanged();
                Filter();
            }
        }

        #endregion

        #region Construction

        protected ChannelBase(string login, string pass, string chanelname, string chanelowner, int ordernum, MainWindowModel model)
        {
            Model = model;
            Login = login;
            Password = pass;
            Application.Current.Dispatcher.Invoke(() => ChanelColor = new SolidColorBrush(Color.FromRgb(255, 255, 255)));
            ChanelName = chanelname;
            ChanelOwner = chanelowner;
            OrderNum = ordernum;
            ListVideoItems = new ObservableCollection<VideoItemBase>();
            ListPopularVideoItems = new ObservableCollection<VideoItemBase>();
            ListSearchVideoItems = new ObservableCollection<VideoItemBase>();
            ListPlaylists = new ObservableCollection<Playlist>();
            ListPopularChannels = new ObservableCollection<ChannelBase>();
            Bgvdb.DoWork += _bgvdb_DoWork;
            Bgvdb.RunWorkerCompleted += _bgvdb_RunWorkerCompleted;
            var fn = new FileInfo(Subscribe.ChanelDb);
            if (fn.Exists)
            {
                var res = Sqllite.GetVideoIntValue(Subscribe.ChanelDb, Sqllite.Isfavorite, Sqllite.Chanelowner, ChanelOwner);
                IsFavorite = res != 0;
            }
            ListVideoItems.CollectionChanged += ListVideoItems_CollectionChanged;
        }

        private void ListVideoItems_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            var collection = sender as ObservableCollection<VideoItemBase>;
            if (collection != null)
                Model.MySubscribe.ResCount = collection.Count;
        }

        protected ChannelBase()
        {
            ChanelName = "All";
            ChanelOwner = "All";
            ChanelType = "All";
        }

        #endregion

        #region Abstract Methods

        public abstract CookieContainer GetSession();

        public abstract void GetItemsFromNet();

        public abstract void AutorizeChanel();

        public abstract void DownloadItem(IList list, bool isAudio);

        public abstract void DownloadItem(VideoItemBase item, bool isGetCookie);

        public abstract void SearchItems(string key, ObservableCollection<VideoItemBase> listSearchVideoItems);

        public abstract void GetPopularItems(string key, ObservableCollection<VideoItemBase> listPopularVideoItems, string mode);

        public abstract void CancelDownloading();

        public abstract void UpdatePlaylist();

        #endregion

        #region Public Methods

        public void GetItemsFromDb()
        {
            var res = Sqllite.GetChanelVideos(Subscribe.ChanelDb, ChanelOwner);
            foreach (DbDataRecord record in res)
            {
                VideoItemBase v = null;
                var servname = record[Sqllite.Servername].ToString();
                if (servname == ChannelYou.Typename)
                    v = new VideoItemYou(record) { Num = ListVideoItems.Count + 1, ParentChanel = this };
                if (servname == ChannelRt.Typename)
                    v = new VideoItemRt(record) { Num = ListVideoItems.Count + 1, ParentChanel = this , Prefix = Prefix};
                if (servname == ChannelTap.Typename)
                    v = new VideoItemTap(record) { Num = ListVideoItems.Count + 1, ParentChanel = this, Prefix = Prefix };
                if (v != null && !ListVideoItems.Contains(v))
                    ListVideoItems.Add(v);
            }

            var lst = new List<VideoItemBase>(ListVideoItems.Count);
            lst.AddRange(ListVideoItems);
            lst = lst.OrderByDescending(x => x.Published).ToList();
            ListVideoItems.Clear();
            foreach (VideoItemBase item in lst)
            {
                ListVideoItems.Add(item);
                item.Num = ListVideoItems.Count;
                item.IsHasFile = item.IsFileExist();
                //item.Delta = item.ViewCount - item.PrevViewCount;
            }
        }

        public void WriteCookiesToDiskBinary(CookieContainer cookieJar, string filename)
        {
            //var subs = ViewModelLocator.MvViewModel.Model.MySubscribe;
            var fn = new FileInfo(Path.Combine(Sqllite.AppDir, filename));
            if (fn.Exists)
            {
                try
                {
                    fn.Delete();
                }
                catch (Exception e)
                {
                    Model.MySubscribe.Result = "WriteCookiesToDiskBinary: " + e.Message;
                }
            }
            using (Stream stream = File.Create(fn.FullName))
            {
                try
                {
                    var formatter = new BinaryFormatter();
                    formatter.Serialize(stream, cookieJar);
                }
                catch (Exception e)
                {
                    Model.MySubscribe.Result = "WriteCookiesToDiskBinary: " + e.Message;
                }
            }
        }

        public static CookieContainer ReadCookiesFromDiskBinary(string filename)
        {
            try
            {
                var fn = new FileInfo(Path.Combine(Sqllite.AppDir, filename));
                if (fn.Exists)
                {
                    using (Stream stream = File.Open(fn.FullName, FileMode.Open))
                    {
                        var formatter = new BinaryFormatter();
                        return (CookieContainer) formatter.Deserialize(stream);
                    }
                }
            }
            catch (Exception ex)
            {
                Subscribe.SetResult("ReadCookiesFromDiskBinary: " + ex.Message);
            }
            return null;
        }

        public void AddToFavorites()
        {
            IsFavorite = !IsFavorite;
            var res = IsFavorite ? 1 : 0;
            Sqllite.UpdateValue(Subscribe.ChanelDb, Sqllite.Isfavorite, res, Sqllite.Chanelowner, ChanelOwner);
        }

        public void DeleteFiles()
        {
            if (SelectedListVideoItems.Count > 0)
            {
                var sb = new StringBuilder();
                foreach (VideoItemBase item in SelectedListVideoItems)
                {
                    if (item.IsHasFile)
                        sb.Append(item.Title).Append(Environment.NewLine);
                }
                var result = MessageBox.Show("Are you sure to delete:" + Environment.NewLine + sb + "?", "Confirm",
                    MessageBoxButton.OKCancel, MessageBoxImage.Information);
                if (result == MessageBoxResult.OK)
                {
                    for (var i = SelectedListVideoItems.Count; i > 0; i--)
                    {
                        var video = SelectedListVideoItems[i - 1] as VideoItemBase;
                        if (video != null && video.IsHasFile)
                        {
                            var fn = new FileInfo(video.FilePath);
                            try
                            {
                                fn.Delete();
                                Subscribe.SetResult("Deleted");
                            }
                            catch (Exception ex)
                            {
                                MessageBox.Show(ex.Message);
                            }
                            video.FilePath = string.Empty;
                            video.IsHasFile = false;
                            video.IsDownLoading = false;
                            video.FileType = "notset";
                        }
                    }
                }
            }
            else
            {
                MessageBox.Show("Please select Video");
            }
        }

        #endregion

        #region Private Methods

        private void _bgvdb_DoWork(object sender, DoWorkEventArgs e)
        {
            switch ((int)e.Argument)
            {
                case 0: //новый канал

                    InsertItemToDb(ListVideoItems);

                    break;

                default: //данные уже есть в базе, надо обновить информацию

                    InsertItemToDb(ListVideoItems.Where(x => x.IsSynced == false).ToList()); //добавим только новые

                    if (IsFull) //в режиме Full - обновим показатели
                    {
                        foreach (VideoItemBase item in ListVideoItems)
                        {
                            Sqllite.UpdateValue(Subscribe.ChanelDb, Sqllite.Viewcount, item.ViewCount, Sqllite.Id, item.VideoID);
                        }
                    }
                    else //обновим только у последних 25 элементов
                    {
                        foreach (VideoItemBase item in ListVideoItems.Take(25))
                        {
                            Sqllite.UpdateValue(Subscribe.ChanelDb, Sqllite.Viewcount, item.ViewCount, Sqllite.Id, item.VideoID);
                        }
                    }

                    break;
            }
        }

        private void _bgvdb_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            if (e.Error == null)
            {
                TimerCommon.Dispose();
                Model.MySubscribe.Result = string.Format("Total: {0}. {1} sync in {2}",
                    Model.MySubscribe.Synctime.ToString(@"mm\:ss"), ChanelName,
                    Synctime.Duration().ToString(@"mm\:ss"));
            }
            else
            {
                Model.MySubscribe.Result = e.Error.Message;
            }
        }

        private void Filter()
        {
            if (CurrentPlaylist == null)
            {
                if (string.IsNullOrEmpty(TitleFilter))
                {
                    if (_filterlist.Any())
                    {
                        ListVideoItems.Clear();
                        foreach (VideoItemBase item in _filterlist)
                        {
                            if (item.Title.Contains(TitleFilter))
                                ListVideoItems.Add(item);
                        }
                    }
                }
                else
                {
                    if (!_filterlist.Any())
                        _filterlist.AddRange(ListVideoItems);
                    ListVideoItems.Clear();
                    foreach (VideoItemBase item in _filterlist)
                    {
                        if (item.Title.ToLower().Contains(TitleFilter.ToLower()))
                            ListVideoItems.Add(item);
                    }
                }
            }
            else
            {
                if (string.IsNullOrEmpty(TitleFilter))
                {
                    if (_filterlist.Any())
                    {
                        ListVideoItems.Clear();

                        if (CurrentPlaylist.ListID == "ALL" && _filterlist.Count != ListVideoItems.Count)
                        {
                            foreach (VideoItemBase item in _filterlist)
                            {
                                ListVideoItems.Add(item);
                            }
                        }
                        else
                        {
                            foreach (VideoItemBase item in _filterlist)
                            {
                                if (item is VideoItemYou)
                                {
                                    if (item.PlaylistID == CurrentPlaylist.ListID)
                                        ListVideoItems.Add(item);
                                }
                                else
                                {
                                    if (item.PlaylistTitle == CurrentPlaylist.Title)
                                        ListVideoItems.Add(item);
                                }
                            }
                        }
                    }
                    else
                    {
                        if (CurrentPlaylist.ListID == "ALL")
                            return;

                        _filterlist.AddRange(ListVideoItems);
                        ListVideoItems.Clear();
                        foreach (VideoItemBase item in _filterlist)
                        {
                            if (item is VideoItemYou)
                            {
                                if (item.PlaylistID == CurrentPlaylist.ListID)
                                    ListVideoItems.Add(item);
                            }
                            else
                            {
                                if (item.PlaylistTitle == CurrentPlaylist.Title)
                                    ListVideoItems.Add(item);
                            }
                        }
                    }

                }
                else
                {
                    if (!_filterlist.Any())
                        _filterlist.AddRange(ListVideoItems);
                    ListVideoItems.Clear();

                    if (CurrentPlaylist.ListID == "ALL")
                    {
                        foreach (VideoItemBase item in _filterlist)
                        {
                            if (item.Title.ToLower().Contains(TitleFilter.ToLower()))
                                ListVideoItems.Add(item);
                        }
                    }
                    else
                    {
                        foreach (VideoItemBase item in _filterlist)
                        {
                            if (item.Title.ToLower().Contains(TitleFilter.ToLower()) &&
                                item.PlaylistID == CurrentPlaylist.ListID)
                                ListVideoItems.Add(item);
                        }
                    }
                }
            }
        }

        private void GetPopularChannelItems()
        {
            if (!(this is ChannelYou)) 
                return;
            var ch = this as ChannelYou;
            if (ch == null) 
                return;
            ch.IsFull = true;
            ch.GetPopularItems(Model.SelectedCountry.Value, ListPopularVideoItems, "PopFill");
        }

        private void InsertItemToDb(IEnumerable<VideoItemBase> lstItems)
        {
            //var clearname = ChanellClearName(ChanelName);
            foreach (VideoItemBase item in lstItems)
            {
                if (string.IsNullOrEmpty(item.VideoID))
                    continue;

                if (item is VideoItemYou)
                {
                    #region Delta

                    //Вычисление дельты - сколько просмотров с предыдущей синхронизации, позволяет находить наиболее часто просматриваемые, но тормозит

                    //VideoItem item1 = item;
                    //Application.Current.Dispatcher.Invoke(() =>
                    //{
                    //    item1.Delta = item1.ViewCount - item1.PrevViewCount;
                    //    item1.PrevViewCount = Sqllite.GetVideoIntValue(Subscribe.ChanelDb, "viewcount", "v_id", item1.VideoID);
                    //});
                    //Sqllite.UpdateValue(Subscribe.ChanelDb, "previewcount", "v_id", item.VideoID, item.PrevViewCount);

                    #endregion

                    Sqllite.InsertRecord(Subscribe.ChanelDb, item.VideoID, ChanelOwner, ChanelName, ChanelType,
                        OrderNum, 0, item.VideoLink, item.Title, item.ViewCount, item.ViewCount, item.Duration,
                        item.Published, item.Description, item.PlaylistID, item.PlaylistTitle);

                    continue;
                }
                if (item is VideoItemRt)
                {
                    var rt = item as VideoItemRt;
                    Sqllite.InsertRecord(Subscribe.ChanelDb, item.VideoID, ChanelOwner, ChanelName, ChanelType,
                        OrderNum, 0, item.VideoLink, item.Title, item.ViewCount, rt.TotalDl, item.Duration,
                        item.Published, item.Description, item.PlaylistID, item.PlaylistTitle);
                }

                if (item is VideoItemTap)
                {
                    var tap = item as VideoItemTap;
                    Sqllite.InsertRecord(Subscribe.ChanelDb, item.VideoID, ChanelOwner, ChanelName, ChanelType,
                        OrderNum, 0, item.VideoLink, item.Title, item.ViewCount, tap.TotalDl, item.Duration,
                        item.Published, item.Description, item.PlaylistID, item.PlaylistTitle);
                }

            }
        }

        //public static string ChanellClearName(string input)
        //{
        //    var rq = new Regex(@"(.+?)(\(\d+\))$");
        //    var match = rq.Match(input);
        //    return match.Success ? rq.Replace(input, "$1").TrimEnd(' ') : input;
        //}

        #endregion
        
    }
}
