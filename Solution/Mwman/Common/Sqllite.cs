using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Data.SQLite;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using Mwman.Video;

namespace Mwman.Common
{
    public class Sqllite
    {
        private const string TableVideos = "tblVideos";

        private const string TableSettings = "tblSettings";

        public static string AppDir;

        #region Columns tblVideos

        public static readonly string Id = "v_id";

        public static readonly string PId = "pl_id";

        public static readonly string PTitle = "ptitle";

        public static readonly string Chanelowner = "chanelowner";

        public static readonly string Chanelname = "chanelname";

        public static readonly string Servername = "servername";

        public static readonly string Ordernum = "ordernum";

        public static readonly string Isfavorite = "isfavorite";

        public static readonly string Url = "url";

        public static readonly string Title = "title";

        public static readonly string Viewcount = "viewcount";

        public static readonly string Previewcount = "previewcount";

        public static readonly string Duration = "duration";

        public static readonly string Published = "published";

        public static readonly string Description = "description";

        public static readonly string Cleartitle = "cleartitle"; 

        #endregion

        #region Columns tblSettings

        public static readonly string Rtlogin = "rtlogin";

        public static readonly string Rtpassword = "rtpassword";

        public static readonly string Taplogin = "taplogin";

        public static readonly string Tappassword = "tappassword";

        public static readonly string Savepath = "savepath";

        public static readonly string Pathtompc = "pathtompc";

        public static readonly string Synconstart = "synconstart";

        public static readonly string Asyncdl = "asyncdl";

        public static readonly string Isonlyfavor = "isonlyfavor";

        public static readonly string Ispopular = "ispopular";

        public static readonly string Pathtoyoudl = "pathtoyoudl";

        public static readonly string Pathtoffmpeg = "pathtoffmpeg";

        public static readonly string Culture = "culture";

        #endregion

        public static void CreateOrConnectDb(string dbfile, string autor, out int totalrow)
        {
            totalrow = 0;
            var fn = new FileInfo(dbfile);

            if (fn.Exists)
            {
                if (fn.Length == 0)
                {
                    fn.Delete();
                    CreateDb(fn.FullName);
                }
                var zap = string.Format("SELECT * FROM {0} WHERE {1}='{2}'", TableVideos, Chanelowner, autor);
                using (var sqlcon = new SQLiteConnection(string.Format("Data Source={0};Version=3;FailIfMissing=True", fn.FullName)))
                using (var sqlcommand = new SQLiteCommand(zap, sqlcon))
                {
                    sqlcon.Open();
                    using (var sdr = sqlcommand.ExecuteReader())
                    {
                        totalrow += sdr.Cast<DbDataRecord>().Count();
                    }
                    sqlcon.Close();
                }
            }
            else
            {
                CreateDb(fn.FullName);
            }
        }

        public static void CreateDb(string dbfile)
        {
            Task t = Task.Run(() =>
            {
                var fnyoudl = new FileInfo(Path.Combine(AppDir, "youtube-dl", "youtube-dl.exe"));
                var fnffmpeg = new FileInfo(Path.Combine(AppDir, "ffmpeg", "ffmpeg.exe"));
                SQLiteConnection.CreateFile(dbfile);
                var lstcom = new List<string>();
                var zap = string.Format(@"CREATE TABLE {0} ({1} TEXT PRIMARY KEY,
                                                        {2} TEXT,
                                                        {3} TEXT,
                                                        {4} TEXT,
                                                        {5} INT,
                                                        {6} INT,
                                                        {7} TEXT,
                                                        {8} TEXT,
                                                        {9} INT,
                                                        {10} INT,
                                                        {11} INT,
                                                        {12} DATETIME,
                                                        {13} TEXT,
                                                        {14} TEXT, 
                                                        {15} TEXT, 
                                                        {16} TEXT)", TableVideos, Id, Chanelowner, Chanelname,
                    Servername, Ordernum, Isfavorite, Url, Title, Viewcount, Previewcount, Duration, Published,
                    Description, Cleartitle, PId, PTitle);
                lstcom.Add(zap);
                var zapdir = string.Format(@"CREATE TABLE {0} ({1} TEXT, 
                                                            {2} TEXT, 
                                                            {3} INT, 
                                                            {4} TEXT, 
                                                            {5} TEXT, 
                                                            {6} INT, 
                                                            {7} INT,
                                                            {8} INT,
                                                            {9} TEXT,
                                                            {10} TEXT,
                                                            {11} TEXT,
                                                            {12} TEXT,
                                                            {13} TEXT)",
                    TableSettings, Savepath, Pathtompc, Synconstart, Pathtoyoudl, Pathtoffmpeg, Isonlyfavor, Ispopular,
                    Asyncdl, Culture, Rtlogin, Rtpassword, Taplogin, Tappassword);
                lstcom.Add(zapdir);
                string insdir;
                if (fnyoudl.Exists & fnffmpeg.Exists)
                {
                    insdir = string.Format(@"INSERT INTO '{0}' ('{1}', '{2}', '{3}', '{4}', '{5}', '{6}', '{7}', '{8}') 
                                                VALUES ('{9}', '0', '0', '0', '1', '{10}', '{11}', 'RU')",
                                                TableSettings, Savepath, Synconstart, Isonlyfavor, Ispopular, Asyncdl, Pathtoyoudl, Pathtoffmpeg, Culture,
                                                Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), fnyoudl.FullName, fnffmpeg.FullName);
                }
                else
                {
                    insdir = string.Format(@"INSERT INTO '{0}' ('{1}', '{2}', '{3}', '{4}', '{5}', '{6}') 
                                                VALUES ('{7}', '0', '0', '0', '1', 'RU')",
                                                TableSettings, Savepath, Synconstart, Isonlyfavor, Ispopular, Asyncdl, Culture,
                                                Environment.GetFolderPath(Environment.SpecialFolder.UserProfile));
                }
                lstcom.Add(insdir);
                using (var sqlcon = new SQLiteConnection(string.Format("Data Source={0};Version=3;FailIfMissing=True", dbfile)))
                    foreach (string com in lstcom)
                    {
                        using (var sqlcommand = new SQLiteCommand(com, sqlcon))
                        {
                            sqlcon.Open();
                            sqlcommand.ExecuteNonQuery();
                            sqlcon.Close();
                        }
                    }
            });
            t.Wait();
        }

        public static void InsertRecord(string dbfile, string id, string chanelowner, string chanelname, string servername, int ordernum, int isfavorite,
            string url, string title, int viewcount, int previewcount, double duration, DateTime published,
            string description, string pid, string ptitle)
        {
            Task t = Task.Run(() =>
            {
                title = title.Replace("'", "''");
                chanelowner = chanelowner.Replace("'", "''");
                var zap =
                    string.Format(
                        @"INSERT INTO '{0}' ('{1}','{2}','{3}','{4}','{5}','{6}','{7}','{8}','{9}','{10}','{11}','{12}','{13}','{14}','{15}','{16}')
                                    VALUES (@{1},@{2},@{3},@{4},@{5},@{6},@{7},@{8},@{9},@{10},@{11},@{12},@{13},@{14},@{15},@{16})",
                        TableVideos,
                        Id,
                        Chanelowner,
                        Chanelname,
                        Servername,
                        Ordernum,
                        Isfavorite,
                        Url,
                        Title,
                        Viewcount,
                        Previewcount,
                        Duration,
                        Published,
                        Description,
                        Cleartitle,
                        PId,
                        PTitle);
                using (
                    var sqlcon =
                        new SQLiteConnection(string.Format("Data Source={0};Version=3;FailIfMissing=True", dbfile))
                    )
                using (var sqlcommand = new SQLiteCommand(sqlcon))
                {
                    sqlcommand.CommandText = zap;
                    sqlcommand.Parameters.AddWithValue("@" + Id, id);
                    sqlcommand.Parameters.AddWithValue("@" + Chanelowner, chanelowner);
                    sqlcommand.Parameters.AddWithValue("@" + Chanelname, chanelname);
                    sqlcommand.Parameters.AddWithValue("@" + Servername, servername);
                    sqlcommand.Parameters.AddWithValue("@" + Ordernum, ordernum);
                    sqlcommand.Parameters.AddWithValue("@" + Isfavorite, isfavorite);
                    sqlcommand.Parameters.AddWithValue("@" + Url, url);
                    sqlcommand.Parameters.AddWithValue("@" + Title, title);
                    sqlcommand.Parameters.AddWithValue("@" + Viewcount, viewcount);
                    sqlcommand.Parameters.AddWithValue("@" + Previewcount, previewcount);
                    sqlcommand.Parameters.AddWithValue("@" + Duration, duration);
                    sqlcommand.Parameters.AddWithValue("@" + Published, published);
                    sqlcommand.Parameters.AddWithValue("@" + Description, description);
                    sqlcommand.Parameters.AddWithValue("@" + Cleartitle, VideoItemBase.MakeValidFileName(title));
                    sqlcommand.Parameters.AddWithValue("@" + PId, pid);
                    sqlcommand.Parameters.AddWithValue("@" + PTitle, ptitle);
                    sqlcon.Open();
                    sqlcommand.ExecuteNonQuery();
                    sqlcon.Close();
                }
            });
            t.Wait();
        }

        public static bool IsTableHasRecord(string dbfile, string id, string chanelowner)
        {
            var res = false;
            Task t = Task.Run(() =>
            {
                var zap = string.Format("SELECT * FROM {0} WHERE {1}='{2}' AND {3}='{4}'", TableVideos, Id, id,
                    Chanelowner, chanelowner);
                using (var sqlcon = new SQLiteConnection(string.Format("Data Source={0};Version=3;FailIfMissing=True", dbfile)))
                using (var sqlcommand = new SQLiteCommand(zap, sqlcon))
                {
                    sqlcon.Open();
                    using (var sdr = sqlcommand.ExecuteReader())
                    {
                        res = sdr.HasRows;
                    }
                    sqlcon.Close();
                }
            });
            t.Wait();
            return res;
        }

        public static Dictionary<string, string> GetDistinctValues(string dbfile, string pid, string ptitle, string chanelowner)
        {
            var res = new Dictionary<string, string>();
            Task t = Task.Run(() =>
            {
                var zap = string.Format("SELECT DISTINCT {0}, {1} FROM {2} WHERE {3}='{4}' ORDER BY {1} ASC", pid, ptitle, TableVideos, Chanelowner, chanelowner);
                using (
                    var sqlcon =
                        new SQLiteConnection(string.Format("Data Source={0};Version=3;FailIfMissing=True", dbfile)))
                using (var sqlcommand = new SQLiteCommand(zap, sqlcon))
                {
                    sqlcon.Open();
                    using (var sdr = sqlcommand.ExecuteReader())
                    {
                        foreach (DbDataRecord record in sdr)
                        {
                            if (!res.ContainsKey(record[pid].ToString()))
                                res.Add(record[pid].ToString(), record[ptitle].ToString());
                        }
                    }
                    sqlcon.Close();
                }
            });
            t.Wait();
            return res;
        }

        public static Dictionary<string, string> GetDistinctValues(string dbfile, string chanelowner, string chanelname, string servername, string ordernum)
        {
            var res = new Dictionary<string, string>();
            Task t = Task.Run(() =>
            {
                var zap = string.Format("SELECT DISTINCT {0}, {1}, {2}, {3} FROM {4} ORDER BY {3} ASC", chanelowner, chanelname, Servername, Ordernum, TableVideos);
                using (
                    var sqlcon =
                        new SQLiteConnection(string.Format("Data Source={0};Version=3;FailIfMissing=True", dbfile)))
                using (var sqlcommand = new SQLiteCommand(zap, sqlcon))
                {
                    sqlcon.Open();
                    using (var sdr = sqlcommand.ExecuteReader())
                    {
                        foreach (DbDataRecord record in sdr)
                        {
                            if (!res.ContainsKey(record[chanelowner].ToString()))
                                res.Add(record[chanelowner].ToString(), record[chanelname] + ":" + record[servername] + ":" + record[ordernum]);
                        }
                    }
                    sqlcon.Close();
                }
            });
            t.Wait();
            return res;
        }

        public static List<DbDataRecord> GetChanelVideos(string dbfile, string chanelowner)
        {
            var res = new List<DbDataRecord>();
            Task t = Task.Run(() =>
            {
                var zap = string.Format("SELECT * FROM {0} WHERE {1}='{2}' ORDER BY {3}", TableVideos, Chanelowner,
                    chanelowner, Published);
                using (var sqlcon = new SQLiteConnection(string.Format("Data Source={0};Version=3;FailIfMissing=True", dbfile)))
                using (var sqlcommand = new SQLiteCommand(zap, sqlcon))
                {
                    sqlcon.Open();
                    using (var sdr = sqlcommand.ExecuteReader())
                    {
                        res.AddRange(sdr.Cast<DbDataRecord>());
                    }
                    sqlcon.Close();
                }
            });
            t.Wait();
            return res;
        }

        public static string GetSettingsValue(string dbfile, string settingname)
        {
            var res = string.Empty;
            Task t = Task.Run(() =>
            {
                var zap = string.Format("SELECT * FROM {0}", TableSettings);
                using (var sqlcon = new SQLiteConnection(string.Format("Data Source={0};Version=3;FailIfMissing=True", dbfile)))
                using (var sqlcommand = new SQLiteCommand(zap, sqlcon))
                {
                    sqlcon.Open();
                    using (var sdr = sqlcommand.ExecuteReader())
                    {
                        if (sdr.HasRows)
                        {
                            while (sdr.Read())
                            {
                                try
                                {
                                    res = sdr[settingname].ToString();
                                }
                                catch (Exception ex)
                                {
                                    MessageBox.Show(string.Format("Check db: {0}{1} {2}", settingname,
                                        Environment.NewLine,
                                        ex.Message));
                                }

                                break;
                            }
                        }
                    }
                    sqlcon.Close();
                }
            });
            t.Wait();
            return res;
        }

        public static int GetSettingsIntValue(string dbfile, string settingname)
        {
            var res = 0;
            Task t = Task.Run(() =>
            {
                var zap = string.Format("SELECT * FROM {0}", TableSettings);
                using (var sqlcon = new SQLiteConnection(string.Format("Data Source={0};Version=3;FailIfMissing=True", dbfile)))
                using (var sqlcommand = new SQLiteCommand(zap, sqlcon))
                {
                    sqlcon.Open();
                    using (var sdr = sqlcommand.ExecuteReader())
                    {
                        if (sdr.HasRows)
                        {
                            while (sdr.Read())
                            {
                                int resu;
                                if (int.TryParse(sdr[settingname].ToString(), out resu))
                                {
                                    res = resu;
                                }
                                break;
                            }
                        }
                    }
                    sqlcon.Close();
                }
            });
            t.Wait();
            return res;
        }

        public static int GetVideoIntValue(string dbfile, string settingname, string keyfield, string key)
        {
            var res = 0;
            Task t = Task.Run(() =>
            {
                var zap = string.Format("SELECT * FROM {0} WHERE {1}='{2}'", TableVideos, keyfield, key);
                using (var sqlcon = new SQLiteConnection(string.Format("Data Source={0};Version=3;FailIfMissing=True", dbfile)))
                using (var sqlcommand = new SQLiteCommand(zap, sqlcon))
                {
                    sqlcon.Open();
                    using (var sdr = sqlcommand.ExecuteReader())
                    {
                        if (sdr.HasRows)
                        {
                            while (sdr.Read())
                            {
                                int resu;
                                if (int.TryParse(sdr[settingname].ToString(), out resu))
                                {
                                    res = resu;
                                }
                                break;
                            }
                        }
                    }
                    sqlcon.Close();
                }
            });
            t.Wait();
            return res;
        }

        public static void UpdateSetting(string dbfile, string settingname, object settingvalue)
        {
            Task t = Task.Run(() =>
            {
                var zap = string.Format("UPDATE {0} SET {1}='{2}'", TableSettings, settingname, settingvalue);
                using (var sqlcon = new SQLiteConnection(string.Format("Data Source={0};Version=3;FailIfMissing=True", dbfile)))
                using (var sqlcommand = new SQLiteCommand(zap, sqlcon))
                {
                    sqlcon.Open();
                    sqlcommand.ExecuteNonQuery();
                    sqlcon.Close();
                }
            });
            t.Wait();
        }

        public static void UpdateChanelOrder(string dbfile, string chanelowner, int neworder)
        {
            Task t = Task.Run(() =>
            {
                var zap = string.Format("UPDATE {0} SET {1}='{2}' WHERE {3}='{4}'", TableVideos, Ordernum, neworder,
                    Chanelowner, chanelowner);
                using (
                    var sqlcon =
                        new SQLiteConnection(string.Format("Data Source={0};Version=3;FailIfMissing=True", dbfile)))
                using (var sqlcommand = new SQLiteCommand(zap, sqlcon))
                {
                    sqlcon.Open();
                    sqlcommand.ExecuteNonQuery();
                    sqlcon.Close();
                }
            });
            t.Wait();
        }

        public static void CreateSettings(string dbfile, string tablename, Dictionary<string, string> columns)
        {
            Task t = Task.Run(() =>
            {
                var sb = new StringBuilder(string.Format("INSERT INTO '{0}' (", tablename));
                foreach (KeyValuePair<string, string> column in columns)
                {
                    sb.AppendFormat("'{0}',", column.Key);
                }
                var tmp = sb.ToString().TrimEnd(',') + ") VALUES (";
                sb = new StringBuilder(tmp);
                foreach (KeyValuePair<string, string> column in columns)
                {
                    sb.AppendFormat("'{0}',", column.Value);
                }

                var zap = sb.ToString().TrimEnd(',') + ")";
                using (var sqlcon = new SQLiteConnection(string.Format("Data Source={0};Version=3;FailIfMissing=True", dbfile)))
                using (var sqlcommand = new SQLiteCommand(zap, sqlcon))
                {
                    sqlcon.Open();
                    sqlcommand.ExecuteNonQuery();
                    sqlcon.Close();
                }
            });
            t.Wait();
        }

        public static void UpdateValue(string dbfile, string valuename, string keyfield, string key, object value)
        {
            Task t = Task.Run(() =>
            {
                var zap = string.Format("UPDATE {0} SET {1}='{2}' WHERE {3}='{4}'", TableVideos, valuename, value,
                    keyfield, key);
                using (var sqlcon = new SQLiteConnection(string.Format("Data Source={0};Version=3;FailIfMissing=True", dbfile)))
                using (var sqlcommand = new SQLiteCommand(zap, sqlcon))
                {
                    sqlcon.Open();
                    sqlcommand.ExecuteNonQuery();
                    sqlcon.Close();
                }
            });
            t.Wait();
        }

        public static void RemoveChanelFromDb(string dbfile, string chanelowner)
        {
            Task t = Task.Run(() =>
            {
                var zap = string.Format("DELETE FROM {0} WHERE {1}='{2}'", TableVideos, Chanelowner, chanelowner);
                using (var sqlcon = new SQLiteConnection(string.Format("Data Source={0};Version=3;FailIfMissing=True", dbfile)))
                using (var sqlcommand = new SQLiteCommand(zap, sqlcon))
                {
                    sqlcon.Open();
                    sqlcommand.ExecuteNonQuery();
                    sqlcon.Close();
                }
            });
            t.Wait();
        }

        public static void UpdateChanelName(string dbfile, string newname, string chanelowner)
        {
            Task t = Task.Run(() =>
            {
                var zap = string.Format("UPDATE {0} SET {1}='{2}' WHERE {3}='{4}'", TableVideos, Chanelname, newname,
                    Chanelowner, chanelowner);
                using (var sqlcon = new SQLiteConnection(string.Format("Data Source={0};Version=3;FailIfMissing=True", dbfile)))
                using (var sqlcommand = new SQLiteCommand(zap, sqlcon))
                {
                    sqlcon.Open();
                    sqlcommand.ExecuteNonQuery();
                    sqlcon.Close();
                }
            });
            t.Wait();
        }

        public static void DropTable(string dbfile, string tablename)
        {
            Task t = Task.Run(() =>
            {
                var zap = string.Format("DROP TABLE {0}", tablename);
                using (var sqlcon = new SQLiteConnection(string.Format("Data Source={0};Version=3;FailIfMissing=True", dbfile)))
                using (var sqlcommand = new SQLiteCommand(zap, sqlcon))
                {
                    sqlcon.Open();
                    sqlcommand.ExecuteNonQuery();
                    sqlcon.Close();
                }
            });
            t.Wait();
        }

        public static void CreateTable(string dbfile, string tablename, Dictionary<string, string> columns)
        {
            Task t = Task.Run(() =>
            {
                var sb = new StringBuilder(string.Format("CREATE TABLE {0} (", tablename));
                foreach (KeyValuePair<string, string> column in columns)
                {
                    sb.AppendFormat("{0} {1},", column.Key, column.Value);
                }
                var zap = sb.ToString().TrimEnd(',') + ")";
                using (var sqlcon = new SQLiteConnection(string.Format("Data Source={0};Version=3;FailIfMissing=True", dbfile)))
                using (var sqlcommand = new SQLiteCommand(zap, sqlcon))
                {
                    sqlcon.Open();
                    sqlcommand.ExecuteNonQuery();
                    sqlcon.Close();
                }
            });
            t.Wait();
        }

    }
}
