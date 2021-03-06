﻿using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Windows;
using System.Xml.Linq;
using Microsoft.Win32;
using Mwman.Channel;
using Mwman.Common;
using Mwman.Views;

namespace Mwman.Models
{
    public class MainWindowModel
    {
        private KeyValuePair<string, string> _selectedCountry;

        public Subscribe MySubscribe { get; set; }

        public ObservableCollection<string> LogCollection { get; set; }

        public List<KeyValuePair<string, string>> Countries { get; set; }

        public string Version { get; set; }

        public KeyValuePair<string, string> SelectedCountry
        {
            get { return _selectedCountry; }
            set
            {
                _selectedCountry = value;
                MySubscribe.TitleFilter = string.Empty;
                MySubscribe.GetPopularVideos(SelectedCountry.Value);
            }
        }

        public MainWindowModel()
        {
            Version = GetVersion();
            MySubscribe = new Subscribe(this);
            LogCollection = new ObservableCollection<string>();

            Countries = new List<KeyValuePair<string, string>>
            {
                new KeyValuePair<string, string>("Russia", "RU"),
                new KeyValuePair<string, string>("Canada", "CA"),
                new KeyValuePair<string, string>("United States", "US"),
                new KeyValuePair<string, string>("Argentina", "AR"),
                new KeyValuePair<string, string>("Australia", "AU"),
                new KeyValuePair<string, string>("Austria", "AT"),
                new KeyValuePair<string, string>("Belgium", "BE"),
                new KeyValuePair<string, string>("Brazil", "BR"),
                new KeyValuePair<string, string>("Chile", "CL"),
                new KeyValuePair<string, string>("Colombia", "CO"),
                new KeyValuePair<string, string>("Czech Republic", "CZ"),
                new KeyValuePair<string, string>("Egypt", "EG"),
                new KeyValuePair<string, string>("France", "FR"),
                new KeyValuePair<string, string>("Germany", "DE"),
                new KeyValuePair<string, string>("Great Britain", "GB"),
                new KeyValuePair<string, string>("Hong Kong", "HK"),
                new KeyValuePair<string, string>("Hungary", "HU"),
                new KeyValuePair<string, string>("India", "IN"),
                new KeyValuePair<string, string>("Ireland", "IE"),
                new KeyValuePair<string, string>("Israel", "IL"),
                new KeyValuePair<string, string>("Italy", "IT"),
                new KeyValuePair<string, string>("Japan", "JP"),
                new KeyValuePair<string, string>("Jordan", "JO"),
                new KeyValuePair<string, string>("Malaysia", "MY"),
                new KeyValuePair<string, string>("Mexico", "MX"),
                new KeyValuePair<string, string>("Morocco", "MA"),
                new KeyValuePair<string, string>("Netherlands", "NL"),
                new KeyValuePair<string, string>("New Zealand", "NZ"),
                new KeyValuePair<string, string>("Peru", "PE"),
                new KeyValuePair<string, string>("Philippines", "PH"),
                new KeyValuePair<string, string>("Poland", "PL"),
                new KeyValuePair<string, string>("Saudi Arabia", "SA"),
                new KeyValuePair<string, string>("Singapore", "SG"),
                new KeyValuePair<string, string>("South Africa", "ZA"),
                new KeyValuePair<string, string>("South Korea", "KR"),
                new KeyValuePair<string, string>("Spain", "ES"),
                new KeyValuePair<string, string>("Sweden", "SE"),
                new KeyValuePair<string, string>("Switzerland", "CH"),
                new KeyValuePair<string, string>("Taiwan", "TW"),
                new KeyValuePair<string, string>("United Arab Emirates", "AE")
            };
        }

        public void OpenSettings(object obj)
        {
            string savepath;
            var mpcpath = string.Empty;
            var synconstart = 0;
            var isonlyfavor = 0;
            var ispopular = 0;
            var isasync = 0;
            var youpath = string.Empty;
            var ffpath = string.Empty;
            var culture = string.Empty;
            
            var fn = new FileInfo(Subscribe.ChanelDb);
            if (fn.Exists)
            {
                savepath = Sqllite.GetSettingsValue(fn.FullName, Sqllite.Savepath);
                mpcpath = Sqllite.GetSettingsValue(fn.FullName, Sqllite.Pathtompc);
                synconstart = Sqllite.GetSettingsIntValue(fn.FullName, Sqllite.Synconstart);
                isonlyfavor = Sqllite.GetSettingsIntValue(fn.FullName, Sqllite.Isonlyfavor);
                ispopular = Sqllite.GetSettingsIntValue(fn.FullName, Sqllite.Ispopular);
                isasync = Sqllite.GetSettingsIntValue(fn.FullName, Sqllite.Asyncdl);
                youpath = Sqllite.GetSettingsValue(fn.FullName, Sqllite.Pathtoyoudl);
                ffpath = Sqllite.GetSettingsValue(fn.FullName, Sqllite.Pathtoffmpeg);
                culture = Sqllite.GetSettingsValue(fn.FullName, Sqllite.Culture);
            }
            else
            {
                savepath = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            }

            try
            {
                var servlist = MySubscribe.ServerList.Where(x => x.ChanelType != "All");
                var settingsModel = new SettingsModel(savepath, mpcpath, synconstart, youpath, ffpath, isonlyfavor, ispopular, isasync, culture, Countries, servlist);
                var settingslView = new SettingsView
                {
                    Owner = Application.Current.MainWindow,
                    DataContext = settingsModel,
                    WindowStartupLocation = WindowStartupLocation.CenterOwner
                };

                settingsModel.View = settingslView;
                settingslView.ShowDialog();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        public void AddLink(object obj)
        {
            try
            {
                if (string.IsNullOrEmpty(Subscribe.YoudlPath))
                {
                    MessageBox.Show("Please set path to Youtube-dl in the Settings", "Info", MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }
                var addmodel = new AddLinkModel(null);
                var addlinkview = new AddLinkView
                {
                    Owner = Application.Current.MainWindow,
                    DataContext = addmodel,
                    WindowStartupLocation = WindowStartupLocation.CenterOwner
                };
                addmodel.View = addlinkview;
                addlinkview.TextBoxLink.Focus();
                addlinkview.ShowDialog();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        public void BackupRestore(object obj)
        {
            switch (obj.ToString())
            {
                case "backup":
                    Backup();
                    break;

                case "restorechanells":
                    RestoreChanells();
                    Subscribe.SetResult("Restore channels completed");
                    break;

                case "restoresettings":
                    RestoreSettings();
                    Subscribe.SetResult("Restore settings comleted");
                    break;
            }
        }

        private static void Backup()
        {
            var dlg = new SaveFileDialog
            {
                FileName = "backup_" + DateTime.Now.ToShortDateString(),
                DefaultExt = ".xml",
                Filter = "XML documents (.xml)|*.xml",
                OverwritePrompt = true
            };
            var res = dlg.ShowDialog();
            if (res == true)
            {
                var doc = new XDocument(new XElement("tables", new XElement("tblSettings",
                    new XElement(Sqllite.Savepath, Sqllite.GetSettingsValue(Subscribe.ChanelDb, Sqllite.Savepath)),
                    new XElement(Sqllite.Pathtompc, Sqllite.GetSettingsValue(Subscribe.ChanelDb, Sqllite.Pathtompc)),
                    new XElement(Sqllite.Synconstart, Sqllite.GetSettingsIntValue(Subscribe.ChanelDb, Sqllite.Synconstart)),
                    new XElement(Sqllite.Isonlyfavor, Sqllite.GetSettingsIntValue(Subscribe.ChanelDb, Sqllite.Isonlyfavor)),
                    new XElement(Sqllite.Ispopular, Sqllite.GetSettingsIntValue(Subscribe.ChanelDb, Sqllite.Ispopular)),
                    new XElement(Sqllite.Asyncdl, Sqllite.GetSettingsIntValue(Subscribe.ChanelDb, Sqllite.Asyncdl)),
                    new XElement(Sqllite.Culture, Sqllite.GetSettingsValue(Subscribe.ChanelDb, Sqllite.Culture)),
                    new XElement(Sqllite.Pathtoyoudl, Sqllite.GetSettingsValue(Subscribe.ChanelDb, Sqllite.Pathtoyoudl)),
                    new XElement(Sqllite.Pathtoffmpeg, Sqllite.GetSettingsValue(Subscribe.ChanelDb, Sqllite.Pathtoffmpeg)),
                    new XElement(Sqllite.Rtlogin, Sqllite.GetSettingsValue(Subscribe.ChanelDb, Sqllite.Rtlogin)),
                    new XElement(Sqllite.Rtpassword, Sqllite.GetSettingsValue(Subscribe.ChanelDb, Sqllite.Rtpassword)),
                    new XElement(Sqllite.Taplogin, Sqllite.GetSettingsValue(Subscribe.ChanelDb, Sqllite.Taplogin)),
                    new XElement(Sqllite.Tappassword, Sqllite.GetSettingsValue(Subscribe.ChanelDb, Sqllite.Tappassword)),
                    new XElement(Sqllite.Youlogin, Sqllite.GetSettingsValue(Subscribe.ChanelDb, Sqllite.Youlogin)),
                    new XElement(Sqllite.Youpassword, Sqllite.GetSettingsValue(Subscribe.ChanelDb, Sqllite.Youpassword)),
                    new XElement(Sqllite.Nnmlogin, Sqllite.GetSettingsValue(Subscribe.ChanelDb, Sqllite.Nnmlogin)),
                    new XElement(Sqllite.Nnmpassword, Sqllite.GetSettingsValue(Subscribe.ChanelDb, Sqllite.Nnmpassword)),
                    new XElement(Sqllite.Plablogin, Sqllite.GetSettingsValue(Subscribe.ChanelDb, Sqllite.Plablogin)),
                    new XElement(Sqllite.Plabpassword, Sqllite.GetSettingsValue(Subscribe.ChanelDb, Sqllite.Plabpassword)),
                    new XElement(Sqllite.Vimeologin, Sqllite.GetSettingsValue(Subscribe.ChanelDb, Sqllite.Vimeologin)),
                    new XElement(Sqllite.Vimeopassword, Sqllite.GetSettingsValue(Subscribe.ChanelDb, Sqllite.Vimeopassword)),
                    new XElement(Sqllite.Kzlogin, Sqllite.GetSettingsValue(Subscribe.ChanelDb, Sqllite.Kzlogin)),
                    new XElement(Sqllite.Kzpassword, Sqllite.GetSettingsValue(Subscribe.ChanelDb, Sqllite.Kzpassword))
                    ), new XElement("tblVideos")));

                var element = doc.Element("tables");
                if (element != null)
                {
                    var xElement = element.Element("tblVideos");
                    if (xElement != null)
                    {
                        foreach (KeyValuePair<string, string> pair in Sqllite.GetDistinctValues(Subscribe.ChanelDb, Sqllite.Chanelowner, Sqllite.Chanelname, Sqllite.Servername, Sqllite.Ordernum))
                        {
                            var sp = pair.Value.Split(':');
                            xElement.Add(new XElement("Chanell", 
                                new XElement(Sqllite.Chanelowner, pair.Key), 
                                new XElement(Sqllite.Chanelname, sp[0]), 
                                new XElement(Sqllite.Servername, sp[1]),
                                new XElement(Sqllite.Ordernum, sp[2])));
                        }
                    }
                }
                doc.Save(dlg.FileName);
                Subscribe.SetResult(string.Format("Backup {0} completed", dlg.FileName));
            }
        }

        private void RestoreChanells()
        {
            var opf = new OpenFileDialog { Filter = "XML documents (.xml)|*.xml" };
            var res = opf.ShowDialog();
            if (res == true)
            {
                try
                {
                    var doc = XDocument.Load(opf.FileName);
                    var xElement1 = doc.Element("tables");
                    if (xElement1 == null) return;
                    var xElement = xElement1.Element("tblVideos");
                    if (xElement == null) return;
                    foreach (XElement element in xElement.Descendants("Chanell"))
                    {
                        var owner = element.Elements().FirstOrDefault(z => z.Name == Sqllite.Chanelowner);
                        var name = element.Elements().FirstOrDefault(z => z.Name == Sqllite.Chanelname);
                        var server = element.Elements().FirstOrDefault(z => z.Name == Sqllite.Servername);
                        var ordernum = element.Elements().FirstOrDefault(z => z.Name == Sqllite.Ordernum);
                        if (owner != null & name != null & server != null & ordernum != null)
                        {
                            ChannelBase chanel = null;
                            if (server.Value == ChannelYou.Typename)
                                chanel = new ChannelYou(string.Empty, string.Empty, name.Value, owner.Value, Convert.ToInt32(ordernum.Value), this);
                            if (server.Value == ChannelRt.Typename)
                                chanel = new ChannelRt(Subscribe.RtLogin, Subscribe.RtPass, name.Value, owner.Value, Convert.ToInt32(ordernum.Value), this);
                            if (server.Value == ChannelTap.Typename)
                                chanel = new ChannelTap(Subscribe.TapLogin, Subscribe.TapPass, name.Value, owner.Value, Convert.ToInt32(ordernum.Value), this);
                            if (chanel != null &&
                                !ViewModelLocator.MvViewModel.Model.MySubscribe.ChanelList.Select(x => x.ChanelOwner)
                                    .Contains(chanel.ChanelOwner))
                            {
                                ViewModelLocator.MvViewModel.Model.MySubscribe.ChanelList.Add(chanel);
                                ViewModelLocator.MvViewModel.Model.MySubscribe.IsOnlyFavorites = false;
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    Subscribe.SetResult(ex.Message);
                }
            }
        }

        private static void RestoreSettings()
        {
            var opf = new OpenFileDialog { Filter = "XML documents (.xml)|*.xml" };
            var res = opf.ShowDialog();
            if (res == true)
            {
                try
                {
                    var doc = XDocument.Load(opf.FileName);
                    var dicv = doc.Descendants("tblSettings").Elements().ToDictionary(setting => setting.Name.LocalName, setting => setting.Value);
                    var dic = new Dictionary<string, string>();
                    foreach (XElement element in doc.Descendants("tblSettings").Elements())
                    {
                        if (element.Name.LocalName == Sqllite.Synconstart 
                            || element.Name.LocalName == Sqllite.Isonlyfavor 
                            || element.Name.LocalName == Sqllite.Ispopular
                            || element.Name.LocalName == Sqllite.Asyncdl)
                            dic.Add(element.Name.LocalName, "INT");
                        else
                            dic.Add(element.Name.LocalName, "TEXT");
                    }
                    Sqllite.DropTable(Subscribe.ChanelDb, "tblSettings");
                    Sqllite.CreateTable(Subscribe.ChanelDb, "tblSettings", dic);
                    Sqllite.CreateSettings(Subscribe.ChanelDb, "tblSettings", dicv);
                    Subscribe.DownloadPath = Sqllite.GetSettingsValue(Subscribe.ChanelDb, Sqllite.Savepath);
                    Subscribe.MpcPath = Sqllite.GetSettingsValue(Subscribe.ChanelDb, Sqllite.Pathtompc);
                    Subscribe.YoudlPath = Sqllite.GetSettingsValue(Subscribe.ChanelDb, Sqllite.Pathtoyoudl);
                    Subscribe.FfmpegPath = Sqllite.GetSettingsValue(Subscribe.ChanelDb, Sqllite.Pathtoffmpeg);
                }
                catch (Exception ex)
                {
                    Subscribe.SetResult(ex.Message);
                }
            }
        }

        private static string GetVersion()
        {
            Assembly assembly = Assembly.GetExecutingAssembly();
            FileVersionInfo fvi = FileVersionInfo.GetVersionInfo(assembly.Location);
            return "Mwman v" + fvi.FileVersion;
        }
    }
}
