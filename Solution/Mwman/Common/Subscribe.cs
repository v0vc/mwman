using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using Mwman.Chanell;
using Mwman.Models;
using Mwman.Video;
using Mwman.Views;
using SevenZip;

namespace Mwman.Common
{
    public class Subscribe : INotifyPropertyChanged
    {
        #region INotifyPropertyChanged

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChangedEventHandler handler = PropertyChanged;
            if (handler != null) handler(this, new PropertyChangedEventArgs(propertyName));
        }

        #endregion

        #region Sites

        public static string RtLogin;

        public static string RtPass;

        public static string TapLogin;

        public static string TapPass;

        #endregion

        #region Settings

        public static string ChanelDb;

        public static string DownloadPath;

        public static string MpcPath;

        public static string YoudlPath;

        public static string FfmpegPath;

        public static bool IsAsyncDl;

        public static bool IsPopular;

        public static bool IsSyncOnStart;

        #endregion

        #region Fields

        private readonly MainWindowModel _model;

        private bool _isOnlyFavorites;

        private string _result;

        //private const string OldDbfile = "ytub.db";

        private const string Dbfile = "mwman.db";

        private ChanelBase _currentChanel;

        private IList _selectedListChanels = new ArrayList();

        private readonly BackgroundWorker _bgv = new BackgroundWorker();

        private readonly List<VideoItemBase> _filterlist = new List<VideoItemBase>();

        private Timer _timer;

        private string _titleFilter;

        private string _chanelFilter;

        private string _searchKey;

        private int _selectedTabIndex;

        private ChanelBase _filterForumItem;

        private int _resCount;

        private ChanelBase _selectedForumItem;

        #endregion

        #region Properties

        public int ResCount
        {
            get { return _resCount; }
            set
            {
                _resCount = value;
                OnPropertyChanged();
            }
        }

        public int SelectedTabIndex
        {
            get { return _selectedTabIndex; }
            set
            {
                _selectedTabIndex = value;
                OnPropertyChanged();
            }
        }

        public ChanelBase CurrentChanel
        {
            get { return _currentChanel; }
            set
            {
                _currentChanel = value;
                OnPropertyChanged();
                if (_currentChanel != null)
                {
                    _model.MySubscribe.ResCount = _currentChanel.ListVideoItems.Count;
                }
            }
        }

        public ChanelBase SelectedForumItem
        {
            get { return _selectedForumItem; }
            set
            {
                _selectedForumItem = value;
                OnPropertyChanged();
            }
        }

        public ChanelBase FilterForumItem
        {
            get { return _filterForumItem; }
            set
            {
                _filterForumItem = value;
                OnPropertyChanged();
                FilterChannel();
            }
        }

        public ObservableCollection<ChanelBase> ChanelList { get; set; }

        public ObservableCollection<ChanelBase> ChanelListToBind { get; set; }

        public ObservableCollection<ChanelBase> ServerList { get; set; }

        public TimeSpan Synctime { get; set; }

        public IList SelectedListChanels
        {
            get { return _selectedListChanels; }
            set
            {
                _selectedListChanels = value;
                OnPropertyChanged();
            }
        }

        public string Result
        {
            get { return _result; }
            set
            {
                _result = value;
                OnPropertyChanged();
            }
        }

        public bool IsOnlyFavorites
        {
            get { return _isOnlyFavorites; }
            set
            {
                _isOnlyFavorites = value;
                OnPropertyChanged();
                if (ChanelList.Any())
                    FilterChannel();
            }
        }

        public string TitleFilter
        {
            get { return _titleFilter; }
            set
            {
                _titleFilter = value;
                OnPropertyChanged();
                FilterVideos();
            }
        }

        public string SearchKey
        {
            get { return _searchKey; }
            set
            {
                _searchKey = value;
                OnPropertyChanged();
            }
        }

        public string ChanelFilter
        {
            get { return _chanelFilter; }
            set
            {
                _chanelFilter = value;
                OnPropertyChanged();
                FilterChannel();
            }
        }

        #endregion

        #region Construction

        public Subscribe(MainWindowModel model)
        {
            _model = model;
            var dir = Path.GetDirectoryName(Process.GetCurrentProcess().MainModule.FileName);
            if (dir == null) return;
            Sqllite.AppDir = dir;
            ChanelList = new ObservableCollection<ChanelBase>();
            ChanelListToBind = new ObservableCollection<ChanelBase>();
            SevenZipBase.SetLibraryPath(Path.Combine(dir, "7z.dll"));
            ChanelDb = Path.Combine(dir, Dbfile);
            var fn = new FileInfo(ChanelDb);
            if (fn.Exists)
            {
                Result = "Working...";
                RtLogin = Sqllite.GetSettingsValue(fn.FullName, Sqllite.Rtlogin);
                RtPass = Sqllite.GetSettingsValue(fn.FullName, Sqllite.Rtpassword);
                TapLogin = Sqllite.GetSettingsValue(fn.FullName, Sqllite.Taplogin);
                TapPass = Sqllite.GetSettingsValue(fn.FullName, Sqllite.Tappassword);
                DownloadPath = Sqllite.GetSettingsValue(ChanelDb, Sqllite.Savepath);
                MpcPath = Sqllite.GetSettingsValue(ChanelDb, Sqllite.Pathtompc);
                IsSyncOnStart = Sqllite.GetSettingsIntValue(ChanelDb, Sqllite.Synconstart) != 0;
                IsAsyncDl = Sqllite.GetSettingsIntValue(ChanelDb, Sqllite.Asyncdl) != 0;
                IsOnlyFavorites = Sqllite.GetSettingsIntValue(ChanelDb, Sqllite.Isonlyfavor) != 0;
                IsPopular = Sqllite.GetSettingsIntValue(ChanelDb, Sqllite.Ispopular) != 0;
                YoudlPath = Sqllite.GetSettingsValue(ChanelDb, Sqllite.Pathtoyoudl);
                FfmpegPath = Sqllite.GetSettingsValue(ChanelDb, Sqllite.Pathtoffmpeg);
                ServerList = new ObservableCollection<ChanelBase>
                {
                    new ChanelYou(string.Empty, string.Empty, string.Empty, string.Empty, 0, _model),
                    new ChanelRt(RtLogin, RtPass, string.Empty, string.Empty, 0, _model),
                    new ChanelTap(TapLogin, TapPass, string.Empty, string.Empty, 0, _model),
                    new ChanelEmpty()
                };
            }
            else
            {
                Result = "Ready";
                ServerList = new ObservableCollection<ChanelBase>
                {
                    new ChanelYou(string.Empty, string.Empty, string.Empty, string.Empty, 0, _model),
                    new ChanelRt(string.Empty, string.Empty, string.Empty, string.Empty, 0, _model),
                    new ChanelTap(string.Empty, string.Empty, string.Empty, string.Empty, 0, _model),
                    new ChanelEmpty()
                };
                DownloadPath = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            }
            SelectedForumItem = ServerList.First(x=>x is ChanelYou);
            FilterForumItem = ServerList.First(x => x is ChanelEmpty);
            _bgv.DoWork += _bgv_DoWork;
            _bgv.RunWorkerCompleted += _bgv_RunWorkerCompleted;
        }

        #endregion

        #region Public Methods

        

        public void PlayItem(object obj)
        {
            if (obj == null)
                return;

            switch (obj.ToString())
            {
                case "MainVideo":
                case "MainAudio":
                case "MainTorrent":
                
                    if (CurrentChanel.CurrentVideoItem is VideoItemYou)
                    {
                        var item = CurrentChanel.CurrentVideoItem as VideoItemYou;
                        item.RunFile(item.IsHasFile ? "Local" : "Online");
                    }

                    if (CurrentChanel.CurrentVideoItem is VideoItemRt)
                    {
                        var item = CurrentChanel.CurrentVideoItem as VideoItemRt;
                        item.RunFile(item.IsHasFile ? "Local" : "Online");
                    }

                    if (CurrentChanel.CurrentVideoItem is VideoItemTap)
                    {
                        var item = CurrentChanel.CurrentVideoItem as VideoItemTap;
                        item.RunFile(item.IsHasFile ? "Local" : "Online");
                    }

                    break;

                case "SearchPlay":
                case "PopularPlay":

                    if (SelectedForumItem.CurrentVideoItem is VideoItemYou)
                    {
                        var item = SelectedForumItem.CurrentVideoItem as VideoItemYou;
                        item.RunFile(item.IsHasFile ? "Local" : "Online");
                    }

                    if (SelectedForumItem.CurrentVideoItem is VideoItemRt)
                    {
                        var item = SelectedForumItem.CurrentVideoItem as VideoItemRt;
                        item.RunFile(item.IsHasFile ? "Local" : "Online");
                    }

                    if (SelectedForumItem.CurrentVideoItem is VideoItemTap)
                    {
                        var item = SelectedForumItem.CurrentVideoItem as VideoItemTap;
                        item.RunFile(item.IsHasFile ? "Local" : "Online");
                    }

                    break;

                case "MainPlay":

                    if (CurrentChanel.CurrentVideoItem is VideoItemYou)
                    {
                        var item = CurrentChanel.CurrentVideoItem as VideoItemYou;
                        item.RunFile(item.IsHasFile ? "Local" : "Online");
                    }

                    if (CurrentChanel.CurrentVideoItem is VideoItemRt)
                    {
                        var item = CurrentChanel.CurrentVideoItem as VideoItemRt;
                        item.RunFile(item.IsHasFile ? "Local" : "Online");
                    }

                    if (CurrentChanel.CurrentVideoItem is VideoItemTap)
                    {
                        var item = CurrentChanel.CurrentVideoItem as VideoItemTap;
                        item.RunFile(item.IsHasFile ? "Local" : "Online");
                    }

                    break;

                case "PopularVideo":
                case "PopularAudio":
                case "SearchVideo":
                case "SearchAudio":
                case "SearchTorrent":

                    if (SelectedForumItem.CurrentVideoItem is VideoItemYou)
                    {
                        var item = SelectedForumItem.CurrentVideoItem as VideoItemYou;
                        item.RunFile(item.IsHasFile ? "Local" : "Online");
                    }

                    if (SelectedForumItem.CurrentVideoItem is VideoItemRt)
                    {
                        var item = SelectedForumItem.CurrentVideoItem as VideoItemRt;
                        item.RunFile(item.IsHasFile ? "Local" : "Online");
                    }

                    if (SelectedForumItem.CurrentVideoItem is VideoItemTap)
                    {
                        var item = SelectedForumItem.CurrentVideoItem as VideoItemTap;
                        item.RunFile(item.IsHasFile ? "Local" : "Online");
                    }

                    break;
                    
            }
        }

        public void Download(object obj)
        {
            if (obj == null)
                return;
            IList lsyou;
            switch (obj.ToString())
            {
                case "MainDownload":

                    CurrentChanel.DownloadItem(CurrentChanel.SelectedListVideoItems, false);

                    break;

                case "PopularDownload":

                    var chanelpop = new ChanelYou(_model);

                    chanelpop.DownloadItem(SelectedForumItem.SelectedListVideoItems, false);

                    break;

                case "SearchDownload":
                    
                    ChanelBase cnanel;
                    lsyou = SelectedForumItem.SelectedListVideoItems.OfType<VideoItemYou>().Select(item => item).ToList();
                    if (lsyou.Count > 0)
                    {
                        cnanel = new ChanelYou(_model);
                        cnanel.DownloadItem(lsyou, false);
                    }

                    IList lsrt = SelectedForumItem.SelectedListVideoItems.OfType<VideoItemRt>().Select(item => item).ToList();
                    if (lsrt.Count > 0)
                    {
                        cnanel = new ChanelRt(_model);
                        cnanel.DownloadItem(lsrt, false);
                    }

                    IList lstap = SelectedForumItem.SelectedListVideoItems.OfType<VideoItemTap>().Select(item => item).ToList();
                    if (lstap.Count > 0)
                    {
                        cnanel = new ChanelTap(_model);
                        cnanel.DownloadItem(lstap, false);
                    }

                    break;

                case "MainInternal":

                    lsyou = CurrentChanel.SelectedListVideoItems.OfType<VideoItemYou>().Select(item => item).ToList();
                    if (lsyou.Count > 0)
                    {
                        cnanel = new ChanelYou(_model);
                        cnanel.DownloadVideoInternal(lsyou);
                    }

                    break;

                case "PopularInternal":
                case "SearchInternal":

                    lsyou = SelectedForumItem.SelectedListVideoItems.OfType<VideoItemYou>().Select(item => item).ToList();
                    if (lsyou.Count > 0)
                    {
                        cnanel = new ChanelYou(_model);
                        cnanel.DownloadVideoInternal(lsyou);
                    }

                    break;
            }
        }

        public void PlayDownload(object obj)
        {
            if (obj == null)
                return;

            IList lsyou;
            switch (obj.ToString())
            {
                case "Search":

                    ChanelBase cnanel;
                    lsyou = SelectedForumItem.SelectedListVideoItems.OfType<VideoItemYou>().Select(item => item).ToList();
                    if (lsyou.Count > 0)
                    {
                        cnanel = new ChanelYou(_model);
                        cnanel.DownloadItem(lsyou, false);
                    }

                    IList lsrt = SelectedForumItem.SelectedListVideoItems.OfType<VideoItemRt>().Select(item => item).ToList();
                    if (lsrt.Count > 0)
                    {
                        cnanel = new ChanelRt(_model);
                        cnanel.DownloadItem(lsrt, false);
                    }

                    IList lstap = SelectedForumItem.SelectedListVideoItems.OfType<VideoItemTap>().Select(item => item).ToList();
                    if (lstap.Count > 0)
                    {
                        cnanel = new ChanelTap(_model);
                        cnanel.DownloadItem(lstap, false);
                    }

                    break;

                case "Popular":

                    var chanelpop = new ChanelYou(_model);

                    chanelpop.DownloadItem(SelectedForumItem.SelectedListVideoItems, false);

                    break;

                case "Get":

                    CurrentChanel.DownloadItem(CurrentChanel.SelectedListVideoItems, false);

                    break;

                case "SearchPlay":
                case "PopularPlay":

                    if (SelectedForumItem.CurrentVideoItem is VideoItemYou)
                    {
                        var item = SelectedForumItem.CurrentVideoItem as VideoItemYou;
                        item.RunFile(item.IsHasFile ? "Local" : "Online");
                    }

                    if (SelectedForumItem.CurrentVideoItem is VideoItemRt)
                    {
                        var item = SelectedForumItem.CurrentVideoItem as VideoItemRt;
                        item.RunFile(item.IsHasFile ? "Local" : "Online");
                    }

                    if (SelectedForumItem.CurrentVideoItem is VideoItemTap)
                    {
                        var item = SelectedForumItem.CurrentVideoItem as VideoItemTap;
                        item.RunFile(item.IsHasFile ? "Local" : "Online");
                    }

                    break;

                case "GetPlay":

                    if (CurrentChanel.CurrentVideoItem is VideoItemYou)
                    {
                        var item = CurrentChanel.CurrentVideoItem as VideoItemYou;
                        item.RunFile(item.IsHasFile ? "Local" : "Online");
                    }

                    if (CurrentChanel.CurrentVideoItem is VideoItemRt)
                    {
                        var item = CurrentChanel.CurrentVideoItem as VideoItemRt;
                        item.RunFile(item.IsHasFile ? "Local" : "Online");
                    }

                    if (CurrentChanel.CurrentVideoItem is VideoItemTap)
                    {
                        var item = CurrentChanel.CurrentVideoItem as VideoItemTap;
                        item.RunFile(item.IsHasFile ? "Local" : "Online");
                    }

                    break;

                case "GetInternal":

                    lsyou = CurrentChanel.SelectedListVideoItems.OfType<VideoItemYou>().Select(item => item).ToList();
                    if (lsyou.Count > 0)
                    {
                        cnanel = new ChanelYou(_model);
                        cnanel.DownloadVideoInternal(lsyou);
                    }

                    break;

                case "PopularInternal":
                case "SearchInternal":

                    lsyou = SelectedForumItem.SelectedListVideoItems.OfType<VideoItemYou>().Select(item => item).ToList();
                    if (lsyou.Count > 0)
                    {
                        cnanel = new ChanelYou(_model);
                        cnanel.DownloadVideoInternal(lsyou);
                    }

                    break;
            }
        }

        public void MoveChanel(object obj)
        {
            if (obj == null)
                return;

            if (!ChanelListToBind.Any() || ChanelListToBind.Count == SelectedListChanels.Count)
                return;

            if (ChanelListToBind.Count != ChanelList.Count) //disable when favorites are on
                return;

            var dic = ChanelListToBind.ToDictionary(chanel => chanel, chanel => ChanelListToBind.IndexOf(chanel));
            int prevIndex;
            int curIndex;
            switch (obj.ToString())
            {
                case "Up":
                    //Местоположение предыдущего элемента
                    prevIndex = -1;

                    for (int i = SelectedListChanels.Count; i > 0; i--)
                    {
                        curIndex = ChanelListToBind.IndexOf((ChanelBase)SelectedListChanels[i - 1]);
                        //Проверка: не выйдет ли элемент за пределы массива
                        if (curIndex > 0)
                        {
                            //Проверка: не займет ли элемент при перемещении место предыдущего элемента
                            if (curIndex - 1 != prevIndex)
                            {
                                ChanelListToBind.Move(curIndex, curIndex - 1);
                            }
                        }
                        //Сохранение местоположения элемента
                        prevIndex = ChanelListToBind.IndexOf((ChanelBase)SelectedListChanels[i - 1]);
                    }

                    break;

                case "Down":
                    prevIndex = ChanelList.Count;
                    for (int i = SelectedListChanels.Count; i > 0; i--)
                    {
                        curIndex = ChanelListToBind.IndexOf((ChanelBase)SelectedListChanels[i - 1]);
                        //Проверка: не выйдет ли элемент за пределы массива
                        if (curIndex < ChanelListToBind.Count - 1)
                        {
                            //Проверка: не займет ли элемент при перемещении место предыдущего элемента
                            if (curIndex + 1 != prevIndex)
                            {
                                ChanelListToBind.Move(curIndex, curIndex + 1);
                            }
                        }
                        //Сохранение местоположения элемента
                        prevIndex = ChanelListToBind.IndexOf((ChanelBase)SelectedListChanels[i - 1]);
                    }

                    break;

            }

            if (obj.ToString() == "SaveOrder") //обновляем все индексы
            {
                foreach (ChanelBase chanel in ChanelListToBind)
                {
                    var index = ChanelListToBind.IndexOf(chanel);
                    Sqllite.UpdateChanelOrder(ChanelDb, chanel.ChanelOwner, index);
                }
                Result = "Order saved";
            }
            else
            {
                foreach (ChanelBase chanel in ChanelListToBind) //обновляем только изменившиеся индексы
                {
                    var index = ChanelListToBind.IndexOf(chanel);
                    int prev;
                    if (dic.TryGetValue(chanel, out prev))
                    {
                        if (prev != index)
                        {
                            Sqllite.UpdateChanelOrder(ChanelDb, chanel.ChanelOwner, index);
                        }
                    }
                }
            }
        }

        public void AddChanel(object o)
        {
            var isEdit = o != null && o.ToString() == "Edit";
            try
            {
                var servlist = new ObservableCollection<ChanelBase>(ServerList.Where(x => x.ChanelType != "All"));
                var addChanelModel = new AddChanelModel(_model, null, isEdit, servlist);
                if (isEdit)
                {
                    addChanelModel.ChanelOwner = CurrentChanel.ChanelOwner;
                    addChanelModel.ChanelName = CurrentChanel.ChanelName;
                    addChanelModel.SelectedForumItem = ServerList.First(z => z.ChanelType == CurrentChanel.ChanelType);
                }

                var addChanelView = new AddChanelView
                {
                    Owner = Application.Current.MainWindow,
                    DataContext = addChanelModel,
                    WindowStartupLocation = WindowStartupLocation.CenterOwner
                };

                addChanelModel.View = addChanelView;

                if (isEdit)
                {
                    addChanelView.TextBoxLink.IsReadOnly = true;
                    addChanelView.TextBoxName.Focus();
                    addChanelView.ComboBoxServers.IsEnabled = false;
                }
                else
                {
                    addChanelView.TextBoxLink.Focus();
                }

                addChanelView.ShowDialog();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        public void AddChanell()
        {
            var ordernum = _model.MySubscribe.ChanelList.Count;
            var item = _model.MySubscribe.SelectedForumItem.CurrentVideoItem;
            if (!_model.MySubscribe.ChanelList.Select(z => z.ChanelOwner).Contains(item.VideoOwner))
            {
                ChanelBase chanel = null;
                if (item is VideoItemYou)
                    chanel = new ChanelYou(RtLogin, RtPass, item.VideoOwner, item.VideoOwner, ordernum, _model);

                if (item is VideoItemRt)
                    chanel = new ChanelRt(RtLogin, RtPass, item.VideoOwnerName, item.VideoOwner, ordernum, _model);

                if (item is VideoItemTap)
                    chanel = new ChanelTap(TapLogin, TapPass, item.VideoOwnerName, item.VideoOwner, ordernum, _model);

                if (chanel != null)
                {
                    _model.MySubscribe.ChanelList.Add(chanel);
                    _model.MySubscribe.ChanelListToBind.Add(chanel);
                    chanel.IsFull = true;
                    chanel.GetItemsFromNet();
                    _model.MySubscribe.CurrentChanel = chanel;
                    _model.MySubscribe.SelectedTabIndex = 0;
                }
            }
            else
            {
                MessageBox.Show("Subscription has already " + item.VideoOwner, "Information", MessageBoxButton.OK,
                    MessageBoxImage.Information);
            }
        }

        public void RemoveChanel(object obj)
        {
            if (SelectedListChanels.Count > 0)
            {
                var sb = new StringBuilder();
                foreach (ChanelBase chanel in SelectedListChanels)
                {
                    sb.Append(chanel.ChanelName).Append(Environment.NewLine);
                }
                var result = MessageBox.Show("Are you sure to delete:" + Environment.NewLine + sb + "?", "Confirm",
                    MessageBoxButton.OKCancel, MessageBoxImage.Information);
                if (result == MessageBoxResult.OK)
                {
                    for (var i = SelectedListChanels.Count; i > 0; i--)
                    {
                        var chanel = SelectedListChanels[i - 1] as ChanelBase;
                        if (chanel == null) continue;
                        Sqllite.RemoveChanelFromDb(ChanelDb, chanel.ChanelOwner);
                        ChanelList.Remove(chanel);
                        ChanelListToBind.Remove(chanel);
                    }
                    Result = "Deleted";
                }
            }
            else
            {
                MessageBox.Show("Please select Chanell");
            }

            if (ChanelList.Any())
                CurrentChanel = ChanelList[0];
        }

        public void GetChanelsFromDb()
        {
            InitializeTimer();

            var fn = new FileInfo(ChanelDb);
            if (!fn.Exists)
            {
                Sqllite.CreateDb(ChanelDb);
                return;
            }

            if (!_bgv.IsBusy)
                _bgv.RunWorkerAsync();
        }

        public void GetPopularVideos(string culture)
        {
            SelectedForumItem.ListPopularVideoItems.Clear();
            if (SelectedForumItem is ChanelYou)
            {
                (SelectedForumItem as ChanelYou).GetPopularItems(culture, SelectedForumItem.ListPopularVideoItems);
            }
            else
            {
                var chanell = new ChanelYou(_model);
                chanell.GetPopularItems(culture, SelectedForumItem.ListPopularVideoItems);    
            }
        }

        public void SearchItems(object obj)
        {
            if (string.IsNullOrEmpty(SearchKey))
                return;

            SelectedForumItem.ListSearchVideoItems.Clear();
            if (SelectedForumItem is ChanelYou)
            {
                (SelectedForumItem as ChanelYou).SearchItems(SearchKey, SelectedForumItem.ListSearchVideoItems);
            }

            if (SelectedForumItem is ChanelRt)
            {
                var chanel = SelectedForumItem as ChanelRt;
                chanel.IsFull = true;
                chanel.SearchItems(SearchKey, chanel.ListSearchVideoItems);
            }

            if (SelectedForumItem is ChanelTap)
            {
                var chanel = SelectedForumItem as ChanelTap;
                chanel.IsFull = true;
                chanel.SearchItems(SearchKey, chanel.ListSearchVideoItems);
            }
        }

        public void SyncChanel(object obj)
        {
            Result = string.Empty;
            Synctime = new TimeSpan();

            Task.Run(() =>
            {
                switch (obj.ToString())
                {
                    case "FullSyncChanelAll":

                        ChanelSync(ChanelList, true);

                        break;

                    case "SyncChanelAll":

                        ChanelSync(ChanelListToBind, false);

                        break;

                    case "SyncChanelSelected":

                        ChanelSync(SelectedListChanels, false);

                        break;

                    case "SyncAllChanelSelected":

                        ChanelSync(SelectedListChanels, true);

                        break;

                    case "SyncChanelFavorites":
                        ChanelSync(ChanelList.Where(x => x.IsFavorite).ToList(), false);
                        break;
                }
            });
        }

        public static void SetResult(string result)
        {
            ViewModelLocator.MvViewModel.Model.MySubscribe.Result = result;
        }

        #endregion

        #region Private Methods

        private void _bgv_DoWork(object sender, DoWorkEventArgs e)
        {
            foreach (KeyValuePair<string, string> pair in Sqllite.GetDistinctValues(ChanelDb, Sqllite.Chanelowner, Sqllite.Chanelname))
            {
                var sp = pair.Value.Split(':');

                ChanelBase chanel = null;
                if (sp[1] == ChanelYou.Typename)
                    chanel = new ChanelYou(string.Empty, string.Empty, sp[0], pair.Key, Convert.ToInt32(sp[2]), _model);
                if (sp[1] == ChanelRt.Typename)
                    chanel = new ChanelRt(RtLogin, RtPass, sp[0], pair.Key, Convert.ToInt32(sp[2]), _model);
                if (sp[1] == ChanelTap.Typename)
                    chanel = new ChanelTap(TapLogin, TapPass, sp[0], pair.Key, Convert.ToInt32(sp[2]), _model);

                ChanelList.Add(chanel);
            }

            foreach (ChanelBase chanel in ChanelList)
            {
                chanel.GetItemsFromDb();
            }

            if (ChanelList.Any())
            {
                CurrentChanel = ChanelList[0];
            }
        }

        private void _bgv_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            _timer.Dispose();
            IsOnlyFavorites = Sqllite.GetSettingsIntValue(ChanelDb, Sqllite.Isonlyfavor) == 1;

            if (IsSyncOnStart)
                SyncChanel(IsOnlyFavorites ? "SyncChanelFavorites" : "SyncChanelAll");

            if (IsPopular)
            {
                var culture = Sqllite.GetSettingsValue(ChanelDb, Sqllite.Culture);
                _model.SelectedCountry = _model.Countries.First(x => x.Value == culture);
            }
            if (ChanelList.Any())
                Result = string.Format("Chanells loaded in {0}", Synctime.Duration().ToString(@"mm\:ss"));
            else
                Result = "Ready";
        }

        private static void ChanelSync(ICollection list, bool isFull)
        {
            if (list == null || list.Count <= 0) return;

            foreach (ChanelBase chanel in list)
            {
                chanel.IsFull = isFull;
                chanel.GetItemsFromNet();
            }
        }

        private void FilterChannel()
        {
            if (Application.Current.Dispatcher.CheckAccess())
                FilterChannelCore();
            else
                Application.Current.Dispatcher.Invoke(FilterChannelCore);
        }

        private void FilterVideos()
        {
            if (Application.Current.Dispatcher.CheckAccess())
                FilterVideosCore();
            else
                Application.Current.Dispatcher.Invoke(FilterVideosCore);
        }

        private void FilterChannelCore()
        {
            ChanelListToBind.Clear();
            if (IsOnlyFavorites)
            {
                if (FilterForumItem is ChanelEmpty)
                {
                    foreach (ChanelBase chanel in ChanelList.Where(x => x.IsFavorite))
                    {
                        if (string.IsNullOrEmpty(ChanelFilter))
                            ChanelListToBind.Add(chanel);
                        else
                        {
                            if (chanel.ChanelName.ToUpper().Contains(ChanelFilter.ToUpper()))
                                ChanelListToBind.Add(chanel);
                        }
                    }
                }
                else
                {
                    foreach (ChanelBase chanel in ChanelList.Where(x => x.IsFavorite & x.ChanelType == FilterForumItem.ChanelType))
                    {
                        if (string.IsNullOrEmpty(ChanelFilter))
                            ChanelListToBind.Add(chanel);
                        else
                        {
                            if (chanel.ChanelName.ToUpper().Contains(ChanelFilter.ToUpper()))
                                ChanelListToBind.Add(chanel);
                        }
                    }
                }
            }
            else
            {
                if (FilterForumItem is ChanelEmpty)
                {
                    foreach (ChanelBase chanel in ChanelList)
                    {
                        if (string.IsNullOrEmpty(ChanelFilter))
                            ChanelListToBind.Add(chanel);
                        else
                        {
                            if (chanel.ChanelName.ToUpper().Contains(ChanelFilter.ToUpper()))
                                ChanelListToBind.Add(chanel);
                        }
                    }    
                }
                else
                {
                    foreach (ChanelBase chanel in ChanelList.Where(x=>x.ChanelType == FilterForumItem.ChanelType))
                    {
                        if (string.IsNullOrEmpty(ChanelFilter))
                            ChanelListToBind.Add(chanel);
                        else
                        {
                            if (chanel.ChanelName.ToUpper().Contains(ChanelFilter.ToUpper()))
                                ChanelListToBind.Add(chanel);
                        }
                    }    
                }
            }
            
            if (ChanelListToBind.Any())
                CurrentChanel = ChanelListToBind[0];
        }

        private void FilterVideosCore()
        {
            if (string.IsNullOrEmpty(TitleFilter))
            {
                if (_filterlist.Any())
                {
                    SelectedForumItem.ListPopularVideoItems.Clear();
                    foreach (VideoItemBase item in _filterlist)
                    {
                        if (item.Title.Contains(TitleFilter))
                            SelectedForumItem.ListPopularVideoItems.Add(item);
                    }
                }
            }
            else
            {
                if (!_filterlist.Any())
                    _filterlist.AddRange(SelectedForumItem.ListPopularVideoItems);
                SelectedForumItem.ListPopularVideoItems.Clear();
                foreach (VideoItemBase item in _filterlist)
                {
                    if (item.Title.ToLower().Contains(TitleFilter.ToLower()))
                        SelectedForumItem.ListPopularVideoItems.Add(item);
                }
            }
            _filterlist.Clear();
        }

        private void tmr_Tick(object o)
        {
            Synctime = Synctime.Add(TimeSpan.FromSeconds(1));
        }

        private void InitializeTimer()
        {
            Synctime = new TimeSpan();
            var tcb = new TimerCallback(tmr_Tick);
            _timer = new Timer(tcb, null, 0, 1000);
        }

        #endregion

        //public static void CheckFfmpegPath()
        //{
        //    if (string.IsNullOrEmpty(FfmpegPath))
        //        IsPathContainFfmpeg = false;
        //    else
        //    {
        //        var fn = new FileInfo(FfmpegPath);
        //        if (fn.Exists && fn.DirectoryName != null)
        //        {
        //            var winpath = Environment.GetEnvironmentVariable("Path", EnvironmentVariableTarget.Machine);
        //            if (winpath != null && winpath.Contains(fn.DirectoryName))
        //                IsPathContainFfmpeg = true;
        //            else
        //                IsPathContainFfmpeg = false;
        //        }
        //        else
        //            IsPathContainFfmpeg = false;
        //    }
        //}

        //public void ShowShutter(bool isShow)
        //{
        //    //this.Send(911); //show shutter
        //    //this.Send(910); //hide shutter
        //    this.Send(isShow ? 911 : 910);
        //}

    }
}
