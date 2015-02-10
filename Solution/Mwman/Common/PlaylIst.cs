namespace Mwman.Common
{
    public class Playlist
    {
        public string Title { get; set; }

        public string ListID { get; set; }

        public string ContentLink { get; set; }

        public Playlist(string title, string listid, string link)
        {
            Title = title;
            ListID = listid;
            ContentLink = link;
        }
    }
}
