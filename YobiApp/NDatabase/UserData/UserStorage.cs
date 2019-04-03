using Common.NDatabase;
using MySql.Data.MySqlClient;

namespace YobiApp.NDatabase.UserData
{
    public class UserStorage : Storage
    {
        public string table = "CREATE TABLE IF NOT EXISTS users" +
        "(" +
            "user_id int AUTO_INCREMENT," +
            "user_state varchar(256)," +
            "user_email varchar(20) NOT NULL," +
            "user_password varchar(20) NOT NULL," +
            "user_hash varchar(100)," +
            "created_at int," +
            "PRIMARY KEY (user_id)" +
        ")";
        public string table_name = "users";

        private string insert = "INSERT INTO users(user_state, user_email, user_password, user_hash, created_at) VALUES(@user_state, @user_email, @user_password, @user_hash, @created_at);";

        public UserStorage(MySqlConnection connection, object locker)
        {
            this.connection = connection;
            this.locker = locker;
            SetTable(table);
            SetTableName(table_name);
        }

        public UserCache Add(UserCache user)
        {
            using (MySqlCommand commandSQL = new MySqlCommand(insert, connection))
            {
                lock (locker)
                {
                    commandSQL.Parameters.AddWithValue("@user_state", user.user_state);
                    commandSQL.Parameters.AddWithValue("@user_email", user.user_email);
                    commandSQL.Parameters.AddWithValue("@user_password", user.user_password);
                    commandSQL.Parameters.AddWithValue("@user_hash", user.user_hash);
                    commandSQL.Parameters.AddWithValue("@created_at", user.created_at);
                    commandSQL.ExecuteNonQuery();
                    user.user_id = (int)commandSQL.LastInsertedId;
                    commandSQL.Dispose();
                    return user;
                }
            }
        }
        public bool UpdateState(int user_id)
        {
            using (MySqlCommand commandSQL = new MySqlCommand("UPDATE users SET user_state='1' WHERE user_id=@user_id", connection))
            {
                commandSQL.Parameters.AddWithValue("@user_id", user_id);
                lock (locker)
                {
                    int updated = commandSQL.ExecuteNonQuery();
                    if (updated > 0)
                    {
                        commandSQL.Dispose();
                        return true;
                    }
                    else
                    {
                        return false;
                    }
                }
            }
        }
        public bool UpdatePassword(int user_id, string user_password)
        {
            using (MySqlCommand commandSQL = new MySqlCommand("UPDATE users SET user_password=@user_password WHERE user_id=@user_id", connection))
            {
                commandSQL.Parameters.AddWithValue("@user_id", user_id); 
                commandSQL.Parameters.AddWithValue("@user_password", user_password); 
                lock (locker)
                {
                    int updated = commandSQL.ExecuteNonQuery();
                    if (updated > 0)
                    {
                        commandSQL.Dispose();
                        return true;
                    }
                    else
                    {
                        return false;
                    }
                }
            }
        }
        public UserCache SelectId(int? user_id)
        {
            using (MySqlCommand commandSQL = new MySqlCommand("SELECT * FROM users WHERE user_id=@user_id;", connection))
            {
                lock (locker)
                {
                    commandSQL.Parameters.AddWithValue("@user_id", user_id);
                    using (MySqlDataReader readerMassive = commandSQL.ExecuteReader())
                    {
                        if (readerMassive.Read())
                        {
                            UserCache user = new UserCache();
                            user.user_id = readerMassive.GetInt32(0);
                            user.user_state = readerMassive.GetInt16(1);
                            user.user_email = readerMassive.GetString(2);
                            user.user_password = readerMassive.GetString(3);
                            user.user_hash = readerMassive.GetString(4);
                            user.created_at = readerMassive.GetInt32(5);
                            commandSQL.Dispose();
                            return user;
                        }
                        else
                        {
                            commandSQL.Dispose();
                            return null;
                        }
                    }
                }
            }
        }
        public UserCache SelectHash(string user_hash)
        {
            using (MySqlCommand commandSQL = new MySqlCommand("SELECT * FROM users WHERE user_hash=@user_hash;", connection))
            {
                lock (locker)
                {
                    commandSQL.Parameters.AddWithValue("@user_hash", user_hash);
                    using (MySqlDataReader readerMassive = commandSQL.ExecuteReader())
                    {
                        if (readerMassive.Read())
                        {
                            UserCache user = new UserCache();
                            user.user_id = readerMassive.GetInt32(0);
                            user.user_state = readerMassive.GetInt16(1);
                            user.user_email = readerMassive.GetString(2);
                            user.user_password = readerMassive.GetString(3);
                            user.user_hash = readerMassive.GetString(4);
                            user.created_at = readerMassive.GetInt32(5);
                            commandSQL.Dispose();
                            return user;
                        }
                        else
                        {
                            commandSQL.Dispose();
                            return null;
                        }
                    }
                }
            }
        }
        public UserCache SelectEmail(string user_email)
        {
            using (MySqlCommand commandSQL = new MySqlCommand("SELECT * FROM users WHERE user_email=@user_email;", connection))
            {
                lock (locker)
                {
                    commandSQL.Parameters.AddWithValue("@user_email", user_email);
                    using (MySqlDataReader readerMassive = commandSQL.ExecuteReader())
                    {
                        if (readerMassive.Read())
                        {
                            UserCache user = new UserCache();
                            user.user_id = readerMassive.GetInt32(0);
                            user.user_state = readerMassive.GetInt16(1);
                            user.user_email = readerMassive.GetString(2);
                            user.user_password = readerMassive.GetString(3);
                            user.user_hash = readerMassive.GetString(4);
                            user.created_at = readerMassive.GetInt32(5);
                            commandSQL.Dispose();
                            return user;
                        }
                        else
                        {
                            commandSQL.Dispose();
                            return null;
                        }
                    }
                }
            }
        }
        public bool Delete(int? user_id)
        {
            using (MySqlCommand commandSQL = new MySqlCommand("DELETE FROM users WHERE user_id=@user_id", connection))
            {
                commandSQL.Parameters.AddWithValue("@user_id", user_id);
                lock (locker)
                {
                    int updated = commandSQL.ExecuteNonQuery();
                    if (updated > 0)
                    {
                        commandSQL.Dispose();
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
