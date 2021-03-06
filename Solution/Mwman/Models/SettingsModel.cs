﻿using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Windows;
using System.Windows.Forms;
using Mwman.Channel;
using Mwman.Common;
using Mwman.Video;
using Mwman.Views;
using SevenZip;
using MessageBox = System.Windows.MessageBox;

namespace Mwman.Models
{
    public class SettingsModel : INotifyPropertyChanged
    {
        #region Fields

        private string _dirpath;

        private string _mpcpath;

        private string _youdlpath;

        private string _ffmpegpath;

        private string _result;

        private bool _isSyncOnStart;

        private bool _isOnlyFavorites;

        private bool _isPopular;

        private int _prValue;

        private bool _isPrVisible;

        private string _youheader;

        private string _ffheader;

        private bool _isAsync;

        #endregion

        #region Commands

        public RelayCommand SaveCommand { get; set; }

        public RelayCommand OpenDirCommand { get; set; }

        public RelayCommand UpdateFileCommand { get; set; }

        #endregion

        #region Properties

        public SettingsView View { get; set; }
        public List<KeyValuePair<string, string>> Countries { get; set; }

        public KeyValuePair<string, string> SelectedCountry { get; set; }

        public ObservableCollection<ChannelBase> ListForums { get; set; }

        public string DirPath
        {
            get { return _dirpath; }
            set
            {
                _dirpath = value;
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

        public string MpcPath
        {
            get { return _mpcpath; }
            set
            {
                _mpcpath = value;
                OnPropertyChanged();
            }
        }

        public string YoudlPath
        {
            get { return _youdlpath; }
            set
            {
                _youdlpath = value;
                OnPropertyChanged();
            }
        }

        public bool IsSyncOnStart
        {
            get { return _isSyncOnStart; }
            set
            {
                _isSyncOnStart = value;
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
            }
        }

        public bool IsAsync
        {
            get { return _isAsync; }
            set
            {
                _isAsync = value;
                OnPropertyChanged();
            }
        }

        public bool IsPopular
        {
            get { return _isPopular; }
            set
            {
                _isPopular = value;
                OnPropertyChanged();
            }
        }

        public int PrValue
        {
            get { return _prValue; }
            set
            {
                _prValue = value;
                OnPropertyChanged();
            }
        }

        public bool IsPrVisible
        {
            get { return _isPrVisible; }
            set
            {
                _isPrVisible = value;
                OnPropertyChanged();
            }
        }

        public string FfmpegPath
        {
            get { return _ffmpegpath; }
            set
            {
                _ffmpegpath = value;
                OnPropertyChanged();
            }
        }

        public string YouHeader
        {
            get { return _youheader; }
            set
            {
                _youheader = value;
                OnPropertyChanged();
            }
        }

        public string FfHeader
        {
            get { return _ffheader; }
            set
            {
                _ffheader = value;
                OnPropertyChanged();
            }
        }

        #endregion

        public SettingsModel(string savepath, string mpcpath, int synconstart, string youpath, string ffmegpath, int isonlyfavor, int ispopular, int isasync, string culture, List<KeyValuePair<string, string>> countries, IEnumerable<ChannelBase> forums)
        {
            MpcPath = string.Empty;
            YoudlPath = string.Empty;
            FfmpegPath = string.Empty;
            DirPath = new DirectoryInfo(savepath).Exists ? savepath : Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            if (!string.IsNullOrEmpty(mpcpath))
                MpcPath = new FileInfo(mpcpath).Exists ? mpcpath : string.Empty;
            if (!string.IsNullOrEmpty(youpath))
                YoudlPath = new FileInfo(youpath).Exists ? youpath : string.Empty;
            if (!string.IsNullOrEmpty(ffmegpath))
                FfmpegPath = new FileInfo(ffmegpath).Exists ? ffmegpath : string.Empty;
            IsSyncOnStart = synconstart == 1;
            IsOnlyFavorites = isonlyfavor == 1;
            IsPopular = ispopular == 1;
            IsAsync = isasync == 1;
            SaveCommand = new RelayCommand(SaveSettings);
            OpenDirCommand = new RelayCommand(OpenDir);
            UpdateFileCommand = new RelayCommand(UpdateFile);
            YouHeader = string.IsNullOrEmpty(YoudlPath) ? "Youtube-dl:" : string.Format("Youtube-dl ({0})", GetVersion(YoudlPath, "--version").Trim());
            FfHeader = string.IsNullOrEmpty(FfmpegPath) ? "FFmpeg:" : string.Format("FFmpeg ({0})", Makeffversion(GetVersion(FfmpegPath, "-version")));
            Countries = countries;
            SelectedCountry = Countries.First(x => x.Value == culture);
            ListForums = new ObservableCollection<ChannelBase>();
            foreach (ChannelBase forum in forums)
            {
                ListForums.Add(forum);
            }
        }

        private void SaveSettings(object obj)
        {
            var ressync = IsSyncOnStart ? 1 : 0;
            Sqllite.UpdateSetting(Subscribe.ChanelDb, Sqllite.Synconstart, ressync);

            var favor = IsOnlyFavorites ? 1 : 0;
            Sqllite.UpdateSetting(Subscribe.ChanelDb, Sqllite.Isonlyfavor, favor);

            var popular = IsPopular ? 1 : 0;
            Sqllite.UpdateSetting(Subscribe.ChanelDb, Sqllite.Ispopular, popular);
            Subscribe.IsPopular = IsPopular;

            var asyncdl = IsAsync ? 1 : 0;
            Sqllite.UpdateSetting(Subscribe.ChanelDb, Sqllite.Asyncdl, asyncdl);
            Subscribe.IsAsyncDl = IsAsync;

            if (IsPopular)
            {
                Sqllite.UpdateSetting(Subscribe.ChanelDb, Sqllite.Culture, SelectedCountry.Value);
            }

            var savedir = new DirectoryInfo(DirPath);
            if (savedir.Exists)
            {
                Sqllite.UpdateSetting(Subscribe.ChanelDb, Sqllite.Savepath, savedir.FullName);
                Subscribe.DownloadPath = savedir.FullName;
                Result = "Saved";
            }
            else
            {
                Result = "Not saved, check Save dir";
            }

            if (!string.IsNullOrEmpty(MpcPath))
            {
                var fn = new FileInfo(MpcPath);
                if (fn.Exists)
                {
                    Sqllite.UpdateSetting(Subscribe.ChanelDb, Sqllite.Pathtompc, fn.FullName);
                    Subscribe.MpcPath = fn.FullName;
                    Result = "Saved";
                }
                else
                {
                    Result = "Not saved, check MPC exe path";
                }
            }

            if (!string.IsNullOrEmpty(YoudlPath))
            {
                var fn = new FileInfo(YoudlPath);
                if (fn.Exists)
                {
                    Sqllite.UpdateSetting(Subscribe.ChanelDb, Sqllite.Pathtoyoudl, fn.FullName);
                    Subscribe.YoudlPath = fn.FullName;
                    Result = "Saved";
                }
                else
                {
                    Result = "Not saved, check Youtube-dl exe path";
                }
            }

            if (!string.IsNullOrEmpty(FfmpegPath))
            {
                var fn = new FileInfo(FfmpegPath);
                if (fn.Exists && fn.DirectoryName != null)
                {
                    #region Windows PATH
                    //var winpath = Environment.GetEnvironmentVariable("Path", EnvironmentVariableTarget.Machine);
                    //if (winpath != null && !winpath.Contains(fn.DirectoryName))
                    //{
                    //    var res = winpath + ";" + fn.DirectoryName;
                    //    try
                    //    {
                    //        Environment.SetEnvironmentVariable("PATH", res, EnvironmentVariableTarget.Machine);
                    //        Subscribe.IsPathContainFfmpeg = true;
                    //    }
                    //    catch
                    //    {
                    //        Subscribe.IsPathContainFfmpeg = false;
                    //        ViewModelLocator.MvViewModel.Model.MySubscribe.Result = "Need admin rights to set ffmpeg into Windows PATH";
                    //    }
                    //}
                    //else
                    //{
                    //    Subscribe.IsPathContainFfmpeg = true;
                    //} 
                    #endregion

                    Sqllite.UpdateSetting(Subscribe.ChanelDb, Sqllite.Pathtoffmpeg, fn.FullName);
                    Subscribe.FfmpegPath = fn.FullName;
                    Result = "Saved";
                }
                else
                {
                    Result = "Not saved, check Ffmpeg exe path";
                }
            }

            foreach (ChannelBase forum in ListForums)
            {
                var login = forum.Login.Trim();
                var pass = forum.Password.Trim();
                if (forum is ChannelRt)
                {
                    Sqllite.UpdateSetting(Subscribe.ChanelDb, Sqllite.Rtlogin, login);
                    Sqllite.UpdateSetting(Subscribe.ChanelDb, Sqllite.Rtpassword, pass);
                    Subscribe.RtLogin = login;
                    Subscribe.RtPass = pass;
                    continue;
                }
                if (forum is ChannelTap)
                {
                    Sqllite.UpdateSetting(Subscribe.ChanelDb, Sqllite.Taplogin, login);
                    Sqllite.UpdateSetting(Subscribe.ChanelDb, Sqllite.Tappassword, pass);
                    Subscribe.TapLogin = login;
                    Subscribe.TapPass = pass;
                    continue;
                }
                if (forum is ChannelYou)
                {
                    Sqllite.UpdateSetting(Subscribe.ChanelDb, Sqllite.Youlogin, login);
                    Sqllite.UpdateSetting(Subscribe.ChanelDb, Sqllite.Youpassword, pass);
                    Subscribe.YouLogin = login;
                    Subscribe.YouPass = pass;
                }
            }

            //View.Close();
        }

        private void OpenDir(object obj)
        {
            if (obj == null) return;

            switch (obj.ToString())
            {
                case "DirPath":
                    var dlg = new FolderBrowserDialog();
                    var res = dlg.ShowDialog();
                    if (res == DialogResult.OK)
                    {
                        DirPath = dlg.SelectedPath;
                    }
                    break;

                case "MpcPath":
                    var dlgf = new OpenFileDialog {Filter = @"EXE files (*.exe)|*.exe"};
                    var resf = dlgf.ShowDialog();
                    if (resf == DialogResult.OK)
                    {
                        MpcPath = dlgf.FileName;
                    }
                    break;

                case "YoudlPath":
                    var dlgy = new OpenFileDialog { Filter = @"EXE files (*.exe)|*.exe" };
                    var resy = dlgy.ShowDialog();
                    if (resy == DialogResult.OK)
                    {
                        YoudlPath = dlgy.FileName;
                        YouHeader = string.Format("Youtube-dl ({0})", GetVersion(YoudlPath, "--version").Trim());

                    }
                    break;

                case "FfmpegPath":
                    var dlgff = new OpenFileDialog { Filter = @"EXE files (*.exe)|*.exe" };
                    var resff = dlgff.ShowDialog();
                    if (resff == DialogResult.OK)
                    {
                        FfmpegPath = dlgff.FileName;
                        FfHeader = string.Format("FFmpeg ({0})", Makeffversion(GetVersion(FfmpegPath, "-version")));
                    }
                    break;
            }
        }

        private void UpdateFile(object obj)
        {
            if (obj == null) return;

            Result = "Dowloading...";

            switch (obj.ToString())
            {
                case "youtube-dl":
                    if (string.IsNullOrEmpty(YoudlPath))
                    {
                        MessageBox.Show("Please set path to Youtube-dl in the Settings", "Info", MessageBoxButton.OK, MessageBoxImage.Information);
                        return;
                    }
                    try
                    {
                        YouHeader = "Youtube-dl:";
                        IsPrVisible = true;
                        var link = (string) Properties.Settings.Default["Youtubedl"];
                        StartDownload(link, YoudlPath, "youtube-dl");
                    }
                    catch (Exception ex)
                    {
                        Result = ex.Message;
                    }
                    
                    
                    break;

                case "ffmpeg":
                    if (string.IsNullOrEmpty(FfmpegPath))
                    {
                        MessageBox.Show("Please set path to ffmpeg in the Settings", "Info", MessageBoxButton.OK, MessageBoxImage.Information);
                        return;
                    }
                    try
                    {
                        var fn = new FileInfo(FfmpegPath);
                        if (fn.Directory != null)
                        {
                            FfHeader = "FFmpeg:";
                            IsPrVisible = true;
                            var dest = Path.Combine(fn.Directory.FullName, "ffmpeg-latest-win32-static.7z");
                            var link = (string)Properties.Settings.Default["FFmpeg"];
                            StartDownload(link, dest, dest);
                        }
                    }
                    catch (Exception ex)
                    {
                        Result = ex.Message;
                    }
                    break;
            }
        }

        private void StartDownload(string url, string dest, string userstate)
        {
            var thread = new Thread(() =>
            {
                var client = new WebClient();
                client.DownloadProgressChanged += ClientOnDownloadProgressChanged;
                client.DownloadFileCompleted += ClientOnDownloadFileCompleted;
                client.DownloadFileAsync(new Uri(url), dest, userstate);
            });
            thread.Start();
        }

        private void ClientOnDownloadFileCompleted(object sender, AsyncCompletedEventArgs e)
        {
            if (e.UserState != null)
            {
                if (e.UserState.ToString() == "youtube-dl")
                {
                    YouHeader = string.Format("Youtube-dl ({0})", GetVersion(YoudlPath, "--version").Trim());
                    Result = "Youtube-dl update Completed";
                }
                else
                {
                    var fn = new FileInfo(e.UserState.ToString());
                    if (fn.Exists && fn.Directory != null)
                    {
                        using (var extr = new SevenZipExtractor(fn.FullName) {PreserveDirectoryStructure = false})
                        {
                            var fileList = extr.ArchiveFileNames.Where(d => d.ToLowerInvariant().EndsWith("ffmpeg.exe")).ToList();
                            extr.ExtractionFinished += extr_ExtractionFinished;
                            extr.Extracting += extr_Extracting;
                            PrValue = 0;
                            extr.ExtractFiles(fn.Directory.FullName, fileList.ToArray());
                        }
                        fn.Delete();
                    }
                }
            }
        }

        void extr_Extracting(object sender, ProgressEventArgs e)
        {
            PrValue = e.PercentDelta;
        }

        void extr_ExtractionFinished(object sender, EventArgs e)
        {
            PrValue = 100;
            FfHeader = string.Format("FFmpeg ({0})", Makeffversion(GetVersion(FfmpegPath, "-version")));
            Result = "FFmpeg update Completed";
        }

        private void ClientOnDownloadProgressChanged(object sender, DownloadProgressChangedEventArgs e)
        {
            var bytesIn = double.Parse(e.BytesReceived.ToString(CultureInfo.InvariantCulture));
            var totalBytes = double.Parse(e.TotalBytesToReceive.ToString(CultureInfo.InvariantCulture));
            var percentage = bytesIn/totalBytes*100;
            PrValue = int.Parse(Math.Truncate(percentage).ToString(CultureInfo.InvariantCulture));
        }

        public static string GetVersion(string path, string param)
        {
            var pProcess = new System.Diagnostics.Process
            {
                StartInfo =
                {
                    FileName = path,
                    Arguments = param,
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    CreateNoWindow = true
                }
            };
            try
            {
                pProcess.Start();
                var res = pProcess.StandardOutput.ReadToEnd();
                pProcess.Close();
                return VideoItemBase.MakeValidFileName(res);
            }
            catch
            {
                return "Please, check";
            }
            
        }

        private static string Makeffversion(string ver)
        {
            var res = string.Empty;
            var regex = new Regex(@"version(.*?)Copyright");
            var match = regex.Match(ver);
            if (match.Success && match.Value.Split(' ').Length == 3)
                res = match.Value.Split(' ')[1];
            var regex2 = new Regex(@"built on(.*?)with");
            match = regex2.Match(ver);
            if (match.Success)
            {
                var sp = VideoItemBase.MakeValidFileName(match.Value).Split(' ');
                if (sp.Length == 7)
                {
                    var sb = new StringBuilder(sp[3]);
                    sb.Append(' ').Append(sp[2]).Append(' ');
                    sb.Append(sp[4]).Append(' ');
                    sb.Append(res);
                    res = sb.ToString();
                }
            }
            return res.Trim();
        }

        #region INotifyPropertyChanged

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChangedEventHandler handler = PropertyChanged;
            if (handler != null) handler(this, new PropertyChangedEventArgs(propertyName));
        }

        #endregion
    }
}
