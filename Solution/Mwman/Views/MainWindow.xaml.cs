using System;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Mwman.Chanell;
using Mwman.Common;

namespace Mwman.Views
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public static readonly DependencyProperty DraggedItemProperty = DependencyProperty.Register("DraggedItem", typeof(ChanelBase), typeof(Window));

        public bool IsDragging { get; set; }

        public bool IsEditing { get; set; }

        public ChanelBase DraggedItem
        {
            get { return (ChanelBase)GetValue(DraggedItemProperty); }
            set { SetValue(DraggedItemProperty, value); }
        }

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
            //DataGridChanels.UnselectAll();
        }

        private void Exit_OnClick(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void SyncChanelOnClick(object sender, RoutedEventArgs e)
        {
            ViewModelLocator.MvViewModel.Model.MySubscribe.SyncChanel("SyncChanelSelected");
        }

        private void SyncAllChanelOnClick(object sender, RoutedEventArgs e)
        {
            ViewModelLocator.MvViewModel.Model.MySubscribe.SyncChanel("SyncAllChanelSelected");
        }

        private void AutorizeChanelOnClick(object sender, RoutedEventArgs e)
        {
            ViewModelLocator.MvViewModel.Model.MySubscribe.CurrentChanel.AutorizeChanel();
        }

        private void RemoveChanelOnClick(object sender, RoutedEventArgs e)
        {
            ViewModelLocator.MvViewModel.Model.MySubscribe.RemoveChanel(null);
        }

        private void EditChanelOnClick(object sender, RoutedEventArgs e)
        {
            ViewModelLocator.MvViewModel.Model.MySubscribe.AddChanel("edit");
        }

        private void MainWindow_OnKeyUp(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Insert)
            {
                ViewModelLocator.MvViewModel.Model.AddLink(null);
            }
        }

        private void CopyLinkOnClick(object sender, RoutedEventArgs e)
        {
            var mitem = sender as MenuItem;
            if (mitem == null) return;
            switch (mitem.CommandParameter.ToString())
            {
                case "Popular":
                case "Search":
                    var chanel = ViewModelLocator.MvViewModel.Model.MySubscribe.SelectedForumItem;
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

                case "Get":
                    var cchanel = ViewModelLocator.MvViewModel.Model.MySubscribe.CurrentChanel;
                    if (cchanel != null && cchanel.CurrentVideoItem != null)
                    {
                        try
                        {
                            Clipboard.SetText(cchanel.CurrentVideoItem.VideoLink);
                        }
                        catch
                        {
                        }
                    }
                    break;
            }
        }

        private void CopyAuthorOnClick(object sender, RoutedEventArgs e)
        {
            var mitem = sender as MenuItem;
            if (mitem == null) return;
            switch (mitem.CommandParameter.ToString())
            {
                case "Popular":
                case "Search":
                    var chanel = ViewModelLocator.MvViewModel.Model.MySubscribe.SelectedForumItem;
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

                case "Get":
                    var cchanel = ViewModelLocator.MvViewModel.Model.MySubscribe.CurrentChanel;
                    if (cchanel != null && cchanel.CurrentVideoItem != null)
                    {
                        try
                        {
                            Clipboard.SetText(cchanel.CurrentVideoItem.VideoOwner);
                        }
                        catch
                        {
                        }
                    }
                    break;
            }

        }

        private void DeleteOnClick(object sender, RoutedEventArgs e)
        {
            var mitem = sender as MenuItem;
            if (mitem == null) return;
            switch (mitem.CommandParameter.ToString())
            {
                case "Popular":
                case "Search":
                    var chanel = ViewModelLocator.MvViewModel.Model.MySubscribe.SelectedForumItem;
                    if (chanel != null && chanel.CurrentVideoItem != null)
                    {
                        chanel.DeleteFiles();
                    }
                    break;

                case "Get":
                    var cchanel = ViewModelLocator.MvViewModel.Model.MySubscribe.CurrentChanel;
                    if (cchanel != null && cchanel.CurrentVideoItem != null)
                    {
                        cchanel.DeleteFiles();
                    }
                    break;
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

        private void AddChanellOnClick(object sender, RoutedEventArgs e)
        {
            ViewModelLocator.MvViewModel.Model.MySubscribe.AddChanell();
        }

        private void DataGrid_OnSorting(object sender, DataGridSortingEventArgs e)
        {
            e.Column.SortDirection = e.Column.SortDirection ?? ListSortDirection.Ascending;
        }
    }
}
