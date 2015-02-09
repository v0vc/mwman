using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace Mwman.Common
{
    public class Playlist : INotifyPropertyChanged
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

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChangedEventHandler handler = PropertyChanged;
            if (handler != null) handler(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
