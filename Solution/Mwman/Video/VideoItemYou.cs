using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data.Common;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using Mwman.Channel;
using Mwman.Common;
using Mwman.Models;
using Newtonsoft.Json.Linq;
using YoutubeExtractor;

namespace Mwman.Video
{
    public class VideoItemYou : VideoItemBase
    {
        public delegate void MyEventHandler();

        public event MyEventHandler Activate;

        #region Fields

        private readonly BackgroundWorker _bgv;

        //private BackgroundWorker _worker;

        private readonly List<string> _destList = new List<string>();

        private bool _isAudio;

        private readonly List<string> _audExtensions = new List<string> {".m4a", ".aac", ".mp3", ".webm"};

        #endregion

        public VideoItemYou(JToken pair, string plid, string pltitle)
        {
            Title = pair["title"]["$t"].ToString();
            VideoID = pair["media$group"]["yt$videoid"]["$t"].ToString();
            PlaylistID = plid;
            PlaylistTitle = pltitle;
        }

        public VideoItemYou(JToken pair, bool isPopular, string region)
        {
            try
            {
                ServerName = ChannelYou.Typename;
                Title = pair["title"]["$t"].ToString();
                ClearTitle = MakeValidFileName(Title);
                try
                {
                    ViewCount = (int)pair["yt$statistics"]["viewCount"]; //sometimes is missed
                }
                catch
                {
                    ViewCount = 0;
                }
                
                Duration = (int) pair["media$group"]["yt$duration"]["seconds"];
                VideoLink = pair["link"][0]["href"].ToString().Split('&')[0];
                Published = (DateTime) pair["published"]["$t"];
                Region = region;
                var owner = pair["author"][0]["uri"]["$t"].ToString().Split('/');
                VideoOwner = owner[owner.Length - 1];
                VideoOwnerName = VideoOwner;
                SavePath = Path.Combine(Subscribe.DownloadPath, VideoOwner);
                if (!isPopular)
                {
                    var spraw = pair["id"]["$t"].ToString().Split('/');
                    VideoID = spraw[spraw.Length - 1];
                    Description = pair["content"]["$t"].ToString();
                }
                else
                {
                    var spraw = pair["id"]["$t"].ToString().Split(':');
                    VideoID = spraw[spraw.Length - 1];
                    Description = VideoOwner;
                }
                _bgv = new BackgroundWorker();
                _bgv.DoWork += _bgv_DoWork;
                _bgv.RunWorkerCompleted += _bgv_RunWorkerCompleted;
            }
            catch (Exception ex)
            {
                Log(ex.Message);
            }
        }

        public VideoItemYou(DbDataRecord record) : base(record)
        {
            ServerName = ChannelYou.Typename;
            SavePath = !string.IsNullOrEmpty(VideoOwner) ? Path.Combine(Subscribe.DownloadPath, VideoOwner) : Subscribe.DownloadPath;
            MinProgress = 0;
            MaxProgress = 100;
            _bgv = new BackgroundWorker();
            _bgv.DoWork += _bgv_DoWork;
            _bgv.RunWorkerCompleted += _bgv_RunWorkerCompleted;
        }

        public VideoItemYou()
        {
            ServerName = ChannelYou.Typename;
            SavePath = !string.IsNullOrEmpty(VideoOwner) ? Path.Combine(Subscribe.DownloadPath, VideoOwner) : Subscribe.DownloadPath;
            MinProgress = 0;
            MaxProgress = 100;
            _bgv = new BackgroundWorker();
            _bgv.DoWork += _bgv_DoWork;
            _bgv.RunWorkerCompleted += _bgv_RunWorkerCompleted;
        }

        private void _bgv_DoWork(object senderr, DoWorkEventArgs e)
        {
            _isAudio = (bool) e.Argument;

            DownloadFileBgv();
        }

        private void _bgv_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            if (e.Error == null)
            {
                if (!string.IsNullOrEmpty(FilePath))
                {
                    var res = "Finished " + FilePath;
                    Log(res);
                    Subscribe.SetResult(res);
                }
            }
            else
            {
                Subscribe.SetResult("Error: " + e.Error.Message);
            }

            if (Activate != null)
                Activate.Invoke();
        }

        public override void RunFile(object runtype)
        {
            switch (runtype.ToString())
            {
                case "Local":
                    var fn = new FileInfo(FilePath);
                    if (fn.Exists)
                    {
                        Process.Start(fn.FullName);
                    }
                    else
                    {
                        MessageBox.Show("File not exist", "Warning", MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                    break;

                case "Online":
                    if (string.IsNullOrEmpty(Subscribe.MpcPath))
                    {
                        MessageBox.Show("Please select mpc exe file", "Warning", MessageBoxButton.OK, MessageBoxImage.Information);
                        return;
                    }
                    var param = string.Format("{0} /play", VideoLink.Replace("https://", "http://"));
                    var proc = Process.Start(Subscribe.MpcPath, param);
                    if (proc != null) proc.Close();
                    break;
            }
        }

        public override bool IsFileExist()
        {
            string pathvid;
            string pathaud;

            if (string.IsNullOrEmpty(VideoOwner))
            {
                if (!string.IsNullOrEmpty(ClearTitle))
                {
                    pathvid = Path.Combine(Subscribe.DownloadPath, string.Format("{0}.mp4", ClearTitle));
                    var fnvid = new FileInfo(pathvid);
                    if (fnvid.Exists)
                    {
                        if (Application.Current.Dispatcher.CheckAccess())
                            FileType = "video";
                        else
                            Application.Current.Dispatcher.Invoke(() => FileType = "video");
                        FilePath = fnvid.FullName;
                        return fnvid.Exists;
                    }
                    foreach (string extension in _audExtensions)
                    {
                        pathaud = Path.Combine(Subscribe.DownloadPath, string.Format("{0}{1}", ClearTitle, extension));
                        var fnaud = new FileInfo(pathaud);
                        if (fnaud.Exists)
                        {
                            if (Application.Current.Dispatcher.CheckAccess())
                                FileType = "audio";
                            else
                                Application.Current.Dispatcher.Invoke(() => FileType = "audio");
                            FilePath = fnaud.FullName;
                            return fnaud.Exists;
                        }
                    }
                }
                else
                {
                    FilePath = string.Empty;
                    return false;
                }
            }
            else
            {
                pathvid = Path.Combine(Subscribe.DownloadPath, VideoOwner, string.Format("{0}.mp4", ClearTitle));
                var fnvid = new FileInfo(pathvid);
                if (fnvid.Exists)
                {
                    if (Application.Current.Dispatcher.CheckAccess())
                        FileType = "video";
                    else
                        Application.Current.Dispatcher.Invoke(() => FileType = "video");
                    FilePath = fnvid.FullName;
                    return fnvid.Exists;
                }
                foreach (string extension in _audExtensions)
                {
                    pathaud = Path.Combine(Subscribe.DownloadPath, VideoOwner, string.Format("{0}{1}", ClearTitle, extension));
                    var fnaud = new FileInfo(pathaud);
                    if (fnaud.Exists)
                    {
                        if (Application.Current.Dispatcher.CheckAccess())
                            FileType = "audio";
                        else
                            Application.Current.Dispatcher.Invoke(() => FileType = "audio");
                        FilePath = fnaud.FullName;
                        return fnaud.Exists;
                    }
                }
            }

            return false;
        }

        public override double GetTorrentSize(string input)
        {
            throw new NotImplementedException();
        }

        public override void DownloadItem(bool isAudio)
        {
            _destList.Clear();
            IsDownLoading = true;
            if (!_bgv.IsBusy)
                _bgv.RunWorkerAsync(isAudio);
        }

        public void CancelDownloading()
        {
            throw new NotImplementedException();
        }

        public async void DownloadInternal()
        {
            var dir = new DirectoryInfo(SavePath);
            if (!dir.Exists)
                dir.Create();
            IsDownLoading = true;
            IsHasFile = false;
            var videoInfos = DownloadUrlResolver.GetDownloadUrls(VideoLink).OrderByDescending(z => z.Resolution);
            var videoInfo = videoInfos.First(info => info.VideoType == VideoType.Mp4 && info.AudioBitrate != 0);
            if (videoInfo != null)
            {
                if (videoInfo.RequiresDecryption)
                {
                    DownloadUrlResolver.DecryptDownloadUrl(videoInfo);
                }

                var downloader = new VideoDownloader(videoInfo, Path.Combine(SavePath, MakeValidFileName(videoInfo.Title) + videoInfo.VideoExtension));
                downloader.DownloadProgressChanged += (sender, args) => downloader_DownloadProgressChanged(args);
                downloader.DownloadFinished += delegate { downloader_DownloadFinished(downloader); };

                //downloader.DownloadProgressChanged += downloader_DownloadProgressChanged;
                //downloader.DownloadFinished += downloader_DownloadFinished;

                //_worker = new BackgroundWorker { WorkerReportsProgress = true };
                //_worker.ProgressChanged += bgv_ProgressChanged;
                //_worker.DoWork += bgv_DoWork;
                //_worker.RunWorkerCompleted += _worker_RunWorkerCompleted;
                //_worker.RunWorkerAsync(downloader);
                
                
                //var scheduler = TaskScheduler.FromCurrentSynchronizationContext();
                //var token = new CancellationToken();

                //Task.Factory.StartNew(DoWork, downloader, token)
                //    .ContinueWith(x => downloader_DownloadFinished(downloader), token, TaskContinuationOptions.None,
                //        scheduler);

                //Task.Factory.StartNew(downloader.Execute, token)
                //    .ContinueWith(x => downloader_DownloadFinished(downloader), token, TaskContinuationOptions.None,
                //        scheduler);
                await Task.Factory.StartNew(downloader.Execute);
            }
        }

        //void _worker_RunWorkerCompleted(object sender, EventArgs e)
        //{
        //    MessageBox.Show("Filished!");
        //    //FilePath = vd.SavePath;
        //    //Subscribe.SetResult(string.Format("\"{0}\" completed!", vd.Video.Title));
        //    //IsHasFile = IsFileExist();
        //    //throw new NotImplementedException();
        //}

        //void downloader_DownloadFinished(object sender, EventArgs e)
        //{
        //    _worker_RunWorkerCompleted(_worker, e);
        //}

        //void downloader_DownloadProgressChanged(object sender, ProgressEventArgs e)
        //{
        //    _worker.ReportProgress((int) e.ProgressPercentage);
        //}

        //void bgv_DoWork(object sender, DoWorkEventArgs e)
        //{
        //    if (e.Argument == null)
        //        return;
        //    var vd = e.Argument as VideoDownloader;
        //    if (vd != null) 
        //        vd.Execute();
        //}

        //void bgv_ProgressChanged(object sender, ProgressChangedEventArgs e)
        //{
        //    Application.Current.Dispatcher.BeginInvoke((Action)(() =>
        //    {
        //        PercentDownloaded = e.ProgressPercentage;
        //    }));
        //}

        //private void DoWork(object o)
        //{
        //    var downloader = o as VideoDownloader;
        //    if (downloader != null) 
        //        downloader.Execute();

        //    //Pauses current thread. but since this thead isn't our main UI thread
        //    //it will not affect our UI experience.
        //    //Thread.Sleep(3000);
        //}

        private void downloader_DownloadFinished(object sender)
        {
            var vd = sender as VideoDownloader;
            if (vd != null)
            {
                Application.Current.Dispatcher.BeginInvoke((Action)(() =>
                {
                    FilePath = vd.SavePath;
                    Subscribe.SetResult(string.Format("\"{0}\" completed!", vd.Video.Title));
                    IsHasFile = IsFileExist();
                }));
            }
        }

        private void downloader_DownloadProgressChanged(ProgressEventArgs e)
        {
            Application.Current.Dispatcher.BeginInvoke((Action)(() =>
            {
                PercentDownloaded = e.ProgressPercentage;
            }));
        } 

        private void DownloadFileBgv()
        {
            var dir = new DirectoryInfo(SavePath);
            if (!dir.Exists)
                dir.Create();
            //"--restrict-filenames"
            string param;
            if (_isAudio)
                param =
                    String.Format(
                        "-f bestaudio -o {0}\\%(title)s.%(ext)s {1} --no-check-certificate --console-title",
                        SavePath, VideoLink);
            else
                param =
                    String.Format(
                        "-f bestvideo,bestaudio -o {0}\\%(title)s.%(ext)s {1} --no-check-certificate --console-title --restrict-filenames",
                        SavePath, VideoLink);

            var startInfo = new ProcessStartInfo(Subscribe.YoudlPath, param)
            {
                WindowStyle = ProcessWindowStyle.Hidden,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                CreateNoWindow = true
            };

            var process = Process.Start(startInfo);
            if (process != null)
            {
                process.OutputDataReceived += (sender, e) => SetLogAndPercentage(e.Data);
                process.Start();
                process.BeginOutputReadLine();
                process.WaitForExit();
                process.Close();
            }
        }

        private void SetLogAndPercentage(string data)
        {
            if (data == null)
            {
                processDownload_Exited();
                return;
            }
            Task t = Task.Run(() =>
            {

                var dest = GetDestination(data);
                if (!string.IsNullOrEmpty(dest))
                    _destList.Add(dest);
                Log(data);
                Subscribe.SetResult("Working...");
                var percent = GetPercentFromYoudlOutput(data);
                if (Math.Abs(percent) > 0)
                {
                    Application.Current.Dispatcher.Invoke(() => PercentDownloaded = percent);
                }
            });
            t.Wait();
        }

        private static double GetPercentFromYoudlOutput(string input)
        {
            try
            {
                if (string.IsNullOrEmpty(input))
                    return 0;
                var regex = new Regex(@"[0-9][0-9]{0,2}\.[0-9]%", RegexOptions.None);
                var match = regex.Match(input);
                if (match.Success)
                {
                    double res;
                    var str = match.Value.TrimEnd('%').Replace('.', ',');
                    if (double.TryParse(str, out res))
                    {
                        return res;
                    }
                }
                return 0;
            }
            catch (Exception ex)
            {
                Log("GetPercentFromYoudlOutput: " + ex.Message);
                return 0;
            }
        }

        private static string GetDestination(string input)
        {
            try
            {
                var regex = new Regex(@"(\[download\] Destination: )(.+?)(\.(mp4|m4a|webm|flv|mp3))(.+)?");
                var match = regex.Match(input);
                if (match.Success)
                {
                    return regex.Replace(input, "$2$3");
                }
                regex = new Regex(@"(\[download\])(.+?)(\.(mp4|m4a|webm|flv|mp3))(.+)?");
                match = regex.Match(input);
                if (match.Success)
                {
                    return regex.Replace(input, "$2$3");
                }
                return string.Empty;
            }
            catch (Exception ex)
            {
                Log("GetDestination: " + ex.Message);
                return string.Empty;
            }
        }

        private void processDownload_Exited()
        {
            if (_isAudio)
            {
                if (_destList.Any())
                {
                    FilePath = _destList.First();
                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        IsHasFile = true;
                        FileType = "audio";
                    });
                    return;
                }
            }

            var fndl = _destList.Select(s => new FileInfo(s)).Where(fn => fn.Exists).ToList();
            var total = fndl.Count();
            if (total == 0)
            {
                Subscribe.SetResult("Can't download " + VideoLink);
                return;
            }
            if (total == 1)
            {
                Subscribe.SetResult("Can't merge one file " + VideoLink);
                return;
            }
            if (String.IsNullOrEmpty(Subscribe.FfmpegPath))
            {
                Subscribe.SetResult("Please, select ffmpeg.exe for merging " + VideoLink);
                return;
            }

            //var fnvid = fndl.First(x => x.Length == fndl.Max(z => z.Length));
            //var fnaud = fndl.First(x => x.Length == fndl.Min(z => z.Length));

            var fnvid = fndl.FirstOrDefault(x => Path.GetExtension(x.Name) == ".mp4" || Path.GetExtension(x.Name) == ".webm");
            var fnaud = new FileInfo(_destList.First());

            foreach (string extension in _audExtensions)
            {
                fnaud = fndl.FirstOrDefault(x => Path.GetExtension(x.Name) == extension);
                if (fnaud != null && fnaud.Exists)
                    break;
            }
            //var fnaud = fndl.First(x => Path.GetExtension(x.Name) == ".m4a"
            //                            || Path.GetExtension(x.Name) == ".webm"
            //                            || Path.GetExtension(x.Name) == ".aac"
            //                            || Path.GetExtension(x.Name) == ".mp3");

            if (fnvid != null && fnaud != null && fnaud.Exists && fnvid.Exists)
                MergeVideos(fnvid, fnaud);
        }

        private void MergeVideos(FileInfo fnvid, FileInfo fnaud)
        {
            if (fnvid.DirectoryName == null)
                return;
            var vfolder = fnvid.DirectoryName;

            var tempname = Path.Combine(vfolder, "." + fnvid.Name);
            var param = String.Format("-i \"{0}\" -i \"{1}\" -vcodec copy -acodec copy \"{2}\" -y", fnvid.FullName,
                fnaud.FullName, tempname);

            var startInfo = new ProcessStartInfo(Subscribe.FfmpegPath, param)
            {
                WindowStyle = ProcessWindowStyle.Hidden,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                CreateNoWindow = true
            };

            var logg = "Merging:" + Environment.NewLine + fnvid.Name + Environment.NewLine + fnaud.Name;
            Log(logg);

            var process = Process.Start(startInfo);

            if (process != null)
            {
                //process.EnableRaisingEvents = true;
                //process.Exited += delegate { processFfmeg_Exited(tempname, fnvid, fnaud, string.Empty); };
                process.OutputDataReceived += (sender, e) => processFfmeg_Exited(tempname, fnvid, fnaud, e.Data);
                process.Start();
                process.BeginOutputReadLine();
                process.WaitForExit();
                process.Close();
            }
        }

        private void processFfmeg_Exited(string tempname, FileInfo fnvid, FileInfo fnaud, string data)
        {
            if (data != null)
                return;
            var fnres = new FileInfo(tempname);
            if (fnres.Exists && fnres.DirectoryName != null)
            {
                FileInfo fnn;
                if (string.IsNullOrEmpty(ClearTitle))
                {
                    var filename = SettingsModel.GetVersion(Subscribe.YoudlPath,
                        String.Format("--get-filename -o \"%(title)s.%(ext)s\" {0}", VideoLink));
                    fnn = new FileInfo(Path.Combine(fnres.DirectoryName, filename));
                }
                else
                {
                    fnn = new FileInfo(Path.Combine(fnres.DirectoryName, ClearTitle + Path.GetExtension(fnvid.Name)));
                }

                try
                {
                    FileHelper.RenameFile(fnres, fnn);
                    Thread.Sleep(2000);
                    fnvid.Delete();
                    fnaud.Delete();
                    FilePath = fnn.FullName;
                    
                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        IsHasFile = true;
                        FileType = "video";    
                    });
                }
                catch (Exception ex)
                {
                    throw new Exception(ex.Message);
                }
            }
        }
    }
}
