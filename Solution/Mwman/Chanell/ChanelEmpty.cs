using System;
using System.Collections;
using System.Net;
using Mwman.Controls;
using Mwman.Video;

namespace Mwman.Chanell
{
    public class ChanelEmpty: ChanelBase
    {
        public ChanelEmpty()
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

        public override void DownloadItem(IList list)
        {
            throw new NotImplementedException();
        }

        public override void DownloadItem(VideoItemBase item, bool isGetCookie)
        {
            throw new NotImplementedException();
        }

        public override void SearchItems(string key, ObservableCollectionEx<VideoItemBase> listSearchVideoItems)
        {
            throw new NotImplementedException();
        }

        public override void GetPopularItems(string key, ObservableCollectionEx<VideoItemBase> listPopularVideoItems)
        {
            throw new NotImplementedException();
        }

        public override void DownloadVideoInternal(IList list)
        {
            throw new NotImplementedException();
        }
    }
}
