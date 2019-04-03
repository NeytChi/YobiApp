using System;
using System.Data;
using Common.NDatabase;
using MySql.Data.MySqlClient;
using Common.NDatabase.LogData;
using Common.NDatabase.FileData;
using System.Collections.Generic;
using YobiApp.NDatabase.UserData;
using YobiApp.NDatabase.AppData;

namespace Common
{
    public static class Database
    {
        public static object locker = new object();
        public static string defaultNameDB = "avatar";
        public static MySqlConnectionStringBuilder connectionstring = new MySqlConnectionStringBuilder();
        public static MySqlConnection connection;

        public static AppStorage app;
        public static UserStorage user;
        public static LogStorage log;
        public static FileStorage file;
        public static List<Storage> storages = new List<Storage>();

        public static void Initialization(bool DatabaseExist)
        {
            Console.WriteLine("MySQL connection...");
            if (!DatabaseExist)
            {
                CheckDatabaseExists();
            }
            GetJsonConfig();
            connection = new MySqlConnection(connectionstring.ToString());
            connection.Open();
            SetMainStorages();
            CheckingAllTables();
            Console.WriteLine("MySQL connected.");
        }
        private static void SetMainStorages()
        {
            app = new AppStorage(connection, locker);
            user = new UserStorage(connection, locker);
            log = new LogStorage(connection, locker);
            file = new FileStorage(connection, locker);
            storages.Add(app);
            storages.Add(user);
            storages.Add(log);
            storages.Add(file);
        }
        public static bool GetJsonConfig()
        {
            string Json = GetConfigDatabase();
            connectionstring.SslMode = MySqlSslMode.None;
            connectionstring.ConnectionReset = true;
            if (Json == null)
            {
                connectionstring.Server = "localhost";
                connectionstring.Database = "databasename";
                connectionstring.UserID = "root";
                connectionstring.Password = "root";
                defaultNameDB = "databasename";
                return false;
            }
            else
            {
                var configJson = Newtonsoft.Json.JsonConvert.DeserializeObject<dynamic>(Json);
                connectionstring.Server = configJson["Server"].ToString();
                connectionstring.UserID = configJson["UserID"].ToString();
                connectionstring.Password = configJson["Password"].ToString();
                connectionstring.Database = configJson["Database"].ToString();
                defaultNameDB = configJson["Database"].ToString();
                return true;
            }
        }
        private static string GetConfigDatabase()
        {
            if (System.IO.File.Exists("database.conf"))
            {
                using (var fstream = System.IO.File.OpenRead("database.conf"))
                {
                    byte[] array = new byte[fstream.Length];
                    fstream.Read(array, 0, array.Length);
                    string textFromFile = System.Text.Encoding.Default.GetString(array);
                    fstream.Close();
                    return textFromFile;
                }
            }
            else
            {
                Console.WriteLine("Function getConfigInfoDB() doesn't get database configuration information. Server DB starting with default configuration.");
                return null;
            }
        }
        public static bool CheckingAllTables()
        {
            bool checking = true;
            foreach (Storage storage in storages)
            {
                if (!CheckTableExists(storage.table))
                {
                    checking = false;
                    Console.WriteLine("The table=" + storage.table + " didn't create.");
                }
            }
            Console.WriteLine("The specified tables created.");
            return checking;
        }
        private static bool CheckTableExists(string sqlCreateCommand)
        {
            try
            {
                using (MySqlCommand command = new MySqlCommand(sqlCreateCommand, connection))
                {
                    command.ExecuteNonQuery();
                    command.Dispose();
                    return true;
                }
            }
            catch (Exception e)
            {
                Console.WriteLine("\r\nError function CheckTableExists().\r\n{1}\r\nMessage:\r\n{0}\r\n", e.Message, sqlCreateCommand);
                return false;
            }
        }
        public static bool DropTables()
        {
            foreach (Storage storage in storages)
            {
                string command = string.Format("DROP TABLE {0};", storage.table_name);
                using (MySqlCommand commandSQL = new MySqlCommand(command, connection))
                {
                    commandSQL.ExecuteNonQuery();
                    commandSQL.Dispose();
                }
                Console.WriteLine("Delete table->" + storage.table_name);
            }
            return true;
        }
        public static void CheckDatabaseExists()
        {
            string Json = GetConfigDatabase();
            connectionstring.SslMode = MySqlSslMode.None;
            connectionstring.ConnectionReset = true;
            if (Json == null)
            {
                connectionstring.Server = "localhost";
                connectionstring.UserID = "root";
                connectionstring.Password = "root";
            }
            else
            {
                var configJson = Newtonsoft.Json.JsonConvert.DeserializeObject<dynamic>(Json);
                connectionstring.Server = configJson["Server"].ToString();
                connectionstring.UserID = configJson["UserID"].ToString();
                connectionstring.Password = configJson["Password"].ToString();
                connectionstring.Database = configJson["Database"].ToString();
                defaultNameDB = configJson["Database"].ToString();
            }
            if (connection != null)
            {
                if (connection.State == ConnectionState.Open)
                {
                    connection.Close();
                }
            }
            connection = new MySqlConnection(connectionstring.ToString());
            connection.Open();
            if (connection.State == ConnectionState.Open)
            {
                using (MySqlCommand command = new MySqlCommand("CREATE DATABASE IF NOT EXISTS " + defaultNameDB + ";", connection))
                {
                    command.ExecuteNonQuery();
                    command.Dispose();
                }
            }
            connection.Close();
        }
        public static Storage AddStorage(Storage storage)
        {
            storage.locker = locker;
            storage.connection = connection;
            if (!CheckTableExists(storage.table))
            {
                Console.WriteLine("The table=" + storage.table + " didn't create.");
                return null;
            }
            return storage;
        }
    }
    public interface IDatabaseLogs
    {
        bool InitConnection();
        void AddLogs(Log log);
        List<Log> SelectLogs();
    }
}