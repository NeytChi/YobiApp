using System;
using MySql.Data.MySqlClient;
using Common.NDatabase.FileData;

namespace Common.NDatabase.FileData
{
    public class FileStorage : Storage
    {
        public string table = "CREATE TABLE IF NOT EXISTS files" +
    	"(" +
    	    "file_id bigint AUTO_INCREMENT," +
    	    "file_path varchar(256)," +
            "file_name varchar(20) NOT NULL," +
    	    "file_type varchar(10)," +
    	    "file_extension varchar(10)," +
    	    "PRIMARY KEY (file_id)" +
    	")";
        public string table_name = "files";

        private string insert = "INSERT INTO files(file_path, file_name, file_type, file_extension)" +
            "VALUES ( @file_path, @file_name, @file_type, @file_extension)";

        private string selectbyid = "SELECT * FROM files WHERE file_id=@file_id";

        public FileStorage(MySqlConnection connection, object locker)
        {
            this.connection = connection;
            this.locker = locker;
            SetTable(table);
            SetTableName(table_name);
        }
        public FileD AddFile(FileD file)
        {
            if (file != null)
            {
                lock (locker)
                {
                    using (MySqlCommand sqlCommand = new MySqlCommand(insert, connection))
                    {
                        sqlCommand.Parameters.AddWithValue("@file_path", file.file_path);
                        sqlCommand.Parameters.AddWithValue("@file_name", file.file_name);
                        sqlCommand.Parameters.AddWithValue("@file_type", file.file_type);
                        sqlCommand.Parameters.AddWithValue("@file_extension", file.file_extension);
                        sqlCommand.ExecuteNonQuery();
                        file.file_id = (int)sqlCommand.LastInsertedId;
                        sqlCommand.Dispose();
                    }
                }
                return file;
            }
            return null;
        }
        public FileD SelectById(int file_id)
        {
            lock (locker)
            {
                using (MySqlCommand sqlCommand = new MySqlCommand(selectbyid, connection))
                {
                    using (MySqlDataReader reader = sqlCommand.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            FileD file = new FileD();
                            file.file_id = reader.GetInt32(0);
                            file.file_path = reader.IsDBNull(1) ? "" : reader.GetString(1);
                            file.file_name = reader.GetString(2);
                            file.file_type = reader.IsDBNull(3) ? "" : reader.GetString(3);
                            file.file_extension = reader.IsDBNull(4) ? "" : reader.GetString(4);
                            return file;
                        }
                        else
                        {
                            return null;
                        }
                    }
                }
            }
        }
        public bool DeleteById(int file_id)
        {
            int deleted = 0;
            lock (locker)
            {
                using (MySqlCommand sqlCommand = new MySqlCommand("DELETE FROM files WHERE file_id=@file_id", connection))
                {
                    sqlCommand.Parameters.AddWithValue("@file_id", file_id);
                    deleted = sqlCommand.ExecuteNonQuery();
                    if (deleted != 0)
                    {
                        return true;
                    }
                    else
                    {
                        return false;
                    }
                }
            }
        }
    }
}























