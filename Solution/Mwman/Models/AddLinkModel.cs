using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Windows;
using Mwman.Common;
using Mwman.Video;
using Mwman.Views;

namespace Mwman.Models
{
    public class AddLinkModel :INotifyPropertyChanged
    {
        public RelayCommand GoCommand { get; set; }

        private string _link;

        private bool _isAudio;

        #region Properties

        public AddLinkView View { get; set; }

        public string Link
        {
            get { return _link; }
            set
            {
                _link = value;
                OnPropertyChanged();
            }
        }

        public bool IsAudio
        {
            get { return _isAudio; }
            set
            {
                _isAudio = value;
                OnPropertyChanged();
            }
        }

        #endregion

        public AddLinkModel(AddLinkView view)
        {
            View = view;
            GoCommand = new RelayCommand(x => Go());
            try
            {
                var text = Clipboard.GetData(DataFormats.Text) as string;
                if (string.IsNullOrWhiteSpace(text) || text.Contains(Environment.NewLine))
                    return;
                Link = text;
            }
            catch (Exception ex)
            {
                Link = ex.Message;
            }
        }

        private void Go()
        {
            if (IsValidUrl(Link))
            {
                View.Close();

                if (Link.ToLower().Contains("youtu"))
                {
                    var youitem = new VideoItemYou {VideoLink = Link};
                    youitem.DownloadItem(IsAudio);
                }
                else
                {
                    var param = String.Format("-o {0}\\%(title)s.%(ext)s {1} --no-check-certificate -i", Subscribe.DownloadPath, Link);
                    ThreadPool.QueueUserWorkItem(delegate
                    {
                        var process = Process.Start(Subscribe.YoudlPath, param);
                        if (process != null) 
                            process.Close(); 
                    });
                }
            }
            else
            {
                Link = "Not valid URL";
            }
        }

        private static bool IsValidUrl(string url)
        {
            if (string.IsNullOrEmpty(url))
                return false;
            Uri uri;
            if (!Uri.TryCreate(url, UriKind.RelativeOrAbsolute, out uri) || null == uri)
            {
                return false;
            }
            return true;
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
