using System;
using Common.NDatabase;
using MySql.Data.MySqlClient;
using System.Collections.Generic;

namespace YobiApp.NDatabase.AppData
{
    public class AppStorage : Storage
    {
        public string table = "CREATE TABLE IF NOT EXISTS apps" +
        "(" +
            "app_id int AUTO_INCREMENT," +
            "app_uid int," +
            "app_hash varchar(20)," +
            "install_link varchar(256)," +
            "archive_name varchar(256)," +
            "url_manifest varchar(256)," +
            "app_name varchar(256)," +
            "url_icon varchar(256)," +
            "version varchar(10)," +
            "build varchar(10)," +
            "bundleIdentifier varchar(256)" +
            "created_at int," +
            "PRIMARY KEY (app_id)" +
        ")";
        public string table_name = "apps";

        public string insert = "INSERT INTO apps(app_uid, app_hash, install_link, archive_name, url_manifest, app_name, url_icon, version, build, bundleIdentifier, created_at)" +
            "VALUES (@app_uid, @app_hash, @install_link, @archive_name, @url_manifest, @app_name, @url_icon, @version, @build, @bundleIdentifier, @created_at);";

        public AppStorage(MySqlConnection connection, object locker)
        {
            this.connection = connection;
            this.locker = locker;
            SetTable(table);
            SetTableName(table_name);
        }
        public App Add(App app)
        {
            using (MySqlCommand commandSQL = new MySqlCommand(insert, connection))
            {
                lock (locker)
                {
                    commandSQL.Parameters.AddWithValue("@app_uid", app.user_id);
                    commandSQL.Parameters.AddWithValue("@app_hash", app.app_hash);
                    commandSQL.Parameters.AddWithValue("@install_link", app.install_link);
                    commandSQL.Parameters.AddWithValue("@archive_name", app.archive_name);
                    commandSQL.Parameters.AddWithValue("@url_manifest", app.url_manifest);
                    commandSQL.Parameters.AddWithValue("@app_name", app.app_name);
                    commandSQL.Parameters.AddWithValue("@url_icon", app.url_icon);
                    commandSQL.Parameters.AddWithValue("@version", app.version);
                    commandSQL.Parameters.AddWithValue("@build", app.build);
                    commandSQL.Parameters.AddWithValue("@bundleIdentifier", app.bundleIdentifier);
                    commandSQL.Parameters.AddWithValue("@created_at", app.created_at);
                    commandSQL.ExecuteNonQuery();
                    app.app_id = (int)commandSQL.LastInsertedId;
                    commandSQL.Dispose();
                    return app;
                }
            }
        }
        public App SelectByHash(string app_hash)
        {
            using (MySqlCommand commandSQL = new MySqlCommand("SELECT * FROM apps WHERE app_hash=@app_hash;", connection))
            {
                commandSQL.Parameters.AddWithValue("@app_hash", app_hash);
                lock (locker)
                {
                    using (MySqlDataReader readerMassive = commandSQL.ExecuteReader())
                    {
                        if (readerMassive.Read())
                        {
                            App app = new App();
                            app.app_id = readerMassive.GetInt32(0);
                            app.user_id = readerMassive.GetInt32(1);
                            app.app_hash = readerMassive.GetString(2);
                            app.install_link = readerMassive.GetString(3);
                            app.archive_name = readerMassive.GetString(4);
                            app.url_manifest = readerMassive.GetString(5);
                            app.app_name = readerMassive.GetString(6);
                            app.url_icon = readerMassive.GetString(7);
                            app.version = readerMassive.GetString(8);
                            app.build = readerMassive.GetString(9);
                            app.bundleIdentifier = readerMassive.GetString(10);
                            app.created_at = readerMassive.GetInt32(11);
                            return app;
                        }
                        else
                        {
                            return null;
                        }
                    }
                }
            }
        }
        public App SelectById(int app_id)
        {
            using (MySqlCommand commandSQL = new MySqlCommand("SELECT * FROM apps WHERE app_id=@app_id;", connection))
            {
                commandSQL.Parameters.AddWithValue("@app_id", app_id);
                lock (locker)
                {
                    using (MySqlDataReader readerMassive = commandSQL.ExecuteReader())
                    {
                        if (readerMassive.Read())
                        {
                            App app = new App();
                            app.app_id = readerMassive.GetInt32(0);
                            app.user_id = readerMassive.GetInt32(1);
                            app.app_hash = readerMassive.GetString(2);
                            app.install_link = readerMassive.GetString(3);
                            app.archive_name = readerMassive.GetString(4);
                            app.url_manifest = readerMassive.GetString(5);
                            app.app_name = readerMassive.GetString(6);
                            app.url_icon = readerMassive.GetString(7);
                            app.version = readerMassive.GetString(8);
                            app.build = readerMassive.GetString(9);
                            app.bundleIdentifier = readerMassive.GetString(10);
                            app.created_at = readerMassive.GetInt32(11);
                            return app;
                        }
                        else
                        {
                            return null;
                        }
                    }
                }
            }
        }
        public List<App> SelectByUserId(int? app_uid)
        {
            using (MySqlCommand commandSQL = new MySqlCommand("SELECT * FROM apps WHERE app_uid=@app_uid;", connection))
            {
                commandSQL.Parameters.AddWithValue("@app_uid", app_uid);
                lock (locker)
                {
                    using (MySqlDataReader readerMassive = commandSQL.ExecuteReader())
                    {
                        List<App> apps = new List<App>();
                        while (readerMassive.Read())
                        {
                            App app = new App();
                            app.app_id = readerMassive.GetInt32(0);
                            app.user_id = readerMassive.GetInt32(1);
                            app.app_hash = readerMassive.GetString(2);
                            app.install_link = readerMassive.GetString(3);
                            app.archive_name = readerMassive.GetString(4);
                            app.url_manifest = readerMassive.GetString(5);
                            app.app_name = readerMassive.GetString(6);
                            app.url_icon = readerMassive.GetString(7);
                            app.version = readerMassive.GetString(8);
                            app.build = readerMassive.GetString(9);
                            app.bundleIdentifier = readerMassive.GetString(10);
                            app.created_at = readerMassive.GetInt32(11);
                            apps.Add(app);
                        }
                        return apps;
                    }
                }
            }
        }
        public List<App> SelectLessMassTime(DateTime dateTime)
        {
            int from_time = (int)(dateTime.ToUniversalTime() - new DateTime(1970, 1, 1, 1, 1, 1)).TotalSeconds;
            using (MySqlCommand commandSQL = new MySqlCommand("SELECT * FROM apps WHERE created_at < @created_at;", connection))
            {
                commandSQL.Parameters.AddWithValue("@created_at", from_time);
                lock (locker)
                {
                    using (MySqlDataReader readerMassive = commandSQL.ExecuteReader())
                    {
                        List<App> apps = new List<App>();
                        while (readerMassive.Read())
                        {
                            App app = new App();
                            app.app_id = readerMassive.GetInt32(0);
                            app.user_id = readerMassive.GetInt32(1);
                            app.app_hash = readerMassive.GetString(2);
                            app.install_link = readerMassive.GetString(3);
                            app.archive_name = readerMassive.GetString(4);
                            app.url_manifest = readerMassive.GetString(5);
                            app.app_name = readerMassive.GetString(6);
                            app.url_icon = readerMassive.GetString(7);
                            app.version = readerMassive.GetString(8);
                            app.build = readerMassive.GetString(9);
                            app.bundleIdentifier = readerMassive.GetString(10);
                            app.created_at = readerMassive.GetInt32(11);
                            apps.Add(app);
                        }
                        return apps;
                    }
                }
            }
        }
        public bool Delete(int app_id)
        {
            using (MySqlCommand commandSQL = new MySqlCommand("DELETE FROM apps WHERE app_id=@app_id;", connection))
            {
                commandSQL.Parameters.AddWithValue("@app_id", app_id);
                lock (locker)
                {
                    if (commandSQL.ExecuteNonQuery() > 0)
                    {
                        commandSQL.Dispose();
                        return true;
                    }
                    else
                    {
                        commandSQL.Dispose();
                        return false;
                    }
                }
            }
        }
        public bool Delete(string app_hash)
        {
            using (MySqlCommand commandSQL = new MySqlCommand("DELETE FROM apps WHERE app_hash=@app_hash;", connection))
            {
                commandSQL.Parameters.AddWithValue("@app_hash", app_hash);
                lock (locker)
                {
                    if (commandSQL.ExecuteNonQuery() > 0)
                    {
                        commandSQL.Dispose();
                        return true;
                    }
                    else
                    {
                        commandSQL.Dispose();
                        return false;
                    }
                }
            }
        }
    }
}
