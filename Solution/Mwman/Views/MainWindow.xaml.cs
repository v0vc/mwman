using System;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Mwman.Chanell;

namespace Mwman.Views
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        private void MainWindow_OnLoaded(object sender, RoutedEventArgs e)
        {
            TextBoxChannelFilter.Focus();
            try
            {
                ViewModelLocator.MvViewModel.Model.MySubscribe.GetChanelsFromDb();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.GetBaseException().Message);
            }
        }

        private void Exit_OnClick(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void MainWindow_OnKeyUp(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Insert)
            {
                ViewModelLocator.MvViewModel.Model.AddLink(null);
            }
        }

        private void Favour_OnMouseUp(object sender, MouseButtonEventArgs e)
        {
            var cchanel = ViewModelLocator.MvViewModel.Model.MySubscribe.CurrentChanel;
            if (cchanel != null)
            {
                cchanel.AddToFavorites();
            }
        }

        private void Row_doubleClick(object sender, MouseButtonEventArgs e)
        {
            ViewModelLocator.MvViewModel.Model.MySubscribe.SyncChanel("SyncChanelSelected");
        }

        private void ButtonShowHideFavor_OnClick(object sender, RoutedEventArgs e)
        {
            ViewModelLocator.MvViewModel.Model.MySubscribe.IsOnlyFavorites = !ViewModelLocator.MvViewModel.Model.MySubscribe.IsOnlyFavorites;
        }

        private void Search_OnKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
                ViewModelLocator.MvViewModel.Model.MySubscribe.SearchItems(null);
        }

        private void DataGridChanels_OnGotFocus(object sender, RoutedEventArgs e)
        {
            ViewModelLocator.MvViewModel.Model.MySubscribe.SelectedTabIndex = 0;
        }

        private void DataGrid_OnSorting(object sender, DataGridSortingEventArgs e)
        {
            e.Column.SortDirection = e.Column.SortDirection ?? ListSortDirection.Ascending;
        }

        private void ChanelOnClick(object sender, RoutedEventArgs e)
        {
            var mitem = sender as MenuItem;
            if (mitem == null) return;

            switch (mitem.CommandParameter.ToString())
            {
                case "SyncChanelSelected":
                    ViewModelLocator.MvViewModel.Model.MySubscribe.SyncChanel(mitem.CommandParameter.ToString());
                    break;

                case "SyncAllChanelSelected":
                    ViewModelLocator.MvViewModel.Model.MySubscribe.SyncChanel(mitem.CommandParameter.ToString());
                    break;

                case "Autorize":
                    ViewModelLocator.MvViewModel.Model.MySubscribe.CurrentChanel.AutorizeChanel();
                    break;

                case "Remove":
                    ViewModelLocator.MvViewModel.Model.MySubscribe.RemoveChanel(null);
                    break;

                case "Edit":
                    ViewModelLocator.MvViewModel.Model.MySubscribe.AddChanel(mitem.CommandParameter.ToString());
                    break;
            }
        }

        private void MainOnClick(object sender, RoutedEventArgs e)
        {
            var mitem = sender as MenuItem;
            if (mitem == null) return;

            ChanelBase chanel;

            switch (mitem.CommandParameter.ToString())
            {
                #region Download Auidio

                case "PopularAudio":
                case "SearchAudio":

                    chanel = ViewModelLocator.MvViewModel.Model.MySubscribe.SelectedForumItem;
                    if (chanel != null && chanel.CurrentVideoItem != null)
                    {
                        chanel.DownloadItem(chanel.SelectedListVideoItems, true);
                    }

                    break;

                case "MainAudio":

                    chanel = ViewModelLocator.MvViewModel.Model.MySubscribe.CurrentChanel;
                    if (chanel != null && chanel.CurrentVideoItem != null)
                    {
                        chanel.DownloadItem(chanel.SelectedListVideoItems, true);
                    }

                    break; 

                #endregion

                #region Cancel Downloading

                case "PopularCancel":
                case "SearchCancel":

                    chanel = ViewModelLocator.MvViewModel.Model.MySubscribe.SelectedForumItem;
                    if (chanel != null && chanel.CurrentVideoItem != null)
                    {
                        chanel.CancelDownloading();
                    }

                    break;

                case "MainCancel":

                    chanel = ViewModelLocator.MvViewModel.Model.MySubscribe.CurrentChanel;
                    if (chanel != null && chanel.CurrentVideoItem != null)
                    {
                        chanel.CancelDownloading();
                    }

                    break; 

                #endregion

                #region Subscribe

                case "Subscribe":

                    ViewModelLocator.MvViewModel.Model.MySubscribe.AddChanell();

                    break;

                #endregion

                #region Copy Link

                case "PopularCopyLink":
                case "SearchCopyLink":

                    chanel = ViewModelLocator.MvViewModel.Model.MySubscribe.SelectedForumItem;
                    if (chanel != null && chanel.CurrentVideoItem != null)
                    {
                        try
                        {
                            Clipboard.SetText(chanel.CurrentVideoItem.VideoLink);
                        }
                        catch
                        {
                        }
                    }

                    break;

                case "MainCopyLink":

                    chanel = ViewModelLocator.MvViewModel.Model.MySubscribe.CurrentChanel;
                    if (chanel != null && chanel.CurrentVideoItem != null)
                    {
                        try
                        {
                            Clipboard.SetText(chanel.CurrentVideoItem.VideoLink);
                        }
                        catch
                        {
                        }
                    }

                    break; 

                #endregion

                #region Copy Autor

                case "PopularCopyAutor":
                case "SearchCopyAutor":

                    chanel = ViewModelLocator.MvViewModel.Model.MySubscribe.SelectedForumItem;
                    if (chanel != null && chanel.CurrentVideoItem != null)
                    {
                        try
                        {
                            Clipboard.SetText(chanel.CurrentVideoItem.VideoOwner);
                        }
                        catch
                        {
                        }
                    }

                    break;

                case "MainCopyAutor":

                    chanel = ViewModelLocator.MvViewModel.Model.MySubscribe.CurrentChanel;
                    if (chanel != null && chanel.CurrentVideoItem != null)
                    {
                        try
                        {
                            Clipboard.SetText(chanel.CurrentVideoItem.VideoOwner);
                        }
                        catch
                        {
                        }
                    }

                    break; 

                #endregion

                #region Delete

                case "PopularDelete":
                case "SearchDelete":
                    chanel = ViewModelLocator.MvViewModel.Model.MySubscribe.SelectedForumItem;
                    if (chanel != null && chanel.CurrentVideoItem != null)
                    {
                        chanel.DeleteFiles();
                    }
                    break;

                case "MainDelete":
                    chanel = ViewModelLocator.MvViewModel.Model.MySubscribe.CurrentChanel;
                    if (chanel != null && chanel.CurrentVideoItem != null)
                    {
                        chanel.DeleteFiles();
                    }
                    break; 

                #endregion
            }
        }
    }
}
