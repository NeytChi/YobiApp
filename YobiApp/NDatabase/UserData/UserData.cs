using System;
namespace YobiApp.NDatabase.UserData
{
    public class UserCache
    {
        public int user_id;
        public short user_state = 0;
        public string user_email;
        public string user_password;
        public string user_hash;
        public int created_at;
    }
}
