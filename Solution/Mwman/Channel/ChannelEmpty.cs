using System;
using System.Collections;
using System.Collections.ObjectModel;
using System.Net;
using Mwman.Video;

namespace Mwman.Channel
{
    public class ChannelEmpty: ChannelBase
    {
        public ChannelEmpty()
        {
            LastColumnHeader = "Download";
            ViewSeedColumnHeader = "Views";
            DurationColumnHeader = "Duration";
        }

        public override CookieContainer GetSession()
        {
            throw new NotImplementedException();
        }

        public override void GetItemsFromNet()
        {
            throw new NotImplementedException();
        }

        public override void AutorizeChanel()
        {
            throw new NotImplementedException();
        }

        public override void DownloadItem(IList list, bool isAudio)
        {
            throw new NotImplementedException();
        }

        public override void DownloadItem(VideoItemBase item, bool isGetCookie)
        {
            throw new NotImplementedException();
        }

        public override void SearchItems(string key, ObservableCollection<VideoItemBase> listSearchVideoItems)
        {
            throw new NotImplementedException();
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
    }
}
