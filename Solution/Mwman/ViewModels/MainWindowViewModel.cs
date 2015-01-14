using Mwman.Common;
using Mwman.Models;

namespace Mwman.ViewModels
{
    public class MainWindowViewModel
    {
        public MainWindowModel Model { get; set; }

        public RelayCommand OpenAddChanelCommand { get; set; }

        public RelayCommand SyncChanelCommand { get; set; }

        public RelayCommand OpenSettingsCommand { get; set; }

        public RelayCommand AddLinkCommand { get; set; }

        public RelayCommand RemoveChanelCommand { get; set; }

        public RelayCommand BackupRestoreCommand { get; set; }

        public RelayCommand SearchCommand { get; set; }

        public RelayCommand PlayDownloadCommand { get; set; }

        public RelayCommand MoveChanelCommand { get; set; }

        public MainWindowViewModel(MainWindowModel model)
        {
            Model = model;
            OpenAddChanelCommand = new RelayCommand(Model.MySubscribe.AddChanel);
            SyncChanelCommand = new RelayCommand(Model.MySubscribe.SyncChanel);
            AddLinkCommand = new RelayCommand(Model.AddLink);
            RemoveChanelCommand = new RelayCommand(Model.MySubscribe.RemoveChanel);
            OpenSettingsCommand = new RelayCommand(Model.OpenSettings);
            BackupRestoreCommand = new RelayCommand(Model.BackupRestore);
            SearchCommand = new RelayCommand(Model.MySubscribe.SearchItems);
            PlayDownloadCommand = new RelayCommand(Model.MySubscribe.PlayDownload);
            MoveChanelCommand = new RelayCommand(Model.MySubscribe.MoveChanel);
        }
    }
}
