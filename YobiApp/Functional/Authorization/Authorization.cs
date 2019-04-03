using System;
using Common;
using System.Net.Sockets;
using Newtonsoft.Json.Linq;
using YobiApp.Functional.Pass;
using YobiApp.NDatabase.AppData;
using System.Collections.Generic;
using YobiApp.NDatabase.UserData;

namespace YobiApp.Functional.RegLogin
{
    public class Authorization
    {
        private Validator Validator;
        private Random random = new Random();
        private const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789";

        public Authorization()
        {
            this.Validator = new Validator();
        }
        public void Registration(ref JObject json, ref Socket handleSocket)
        {
            if (json == null) 
            { 
                return; 
            }
            string email = Worker.CheckRequiredJsonField(ref json, "email", JTokenType.String, ref handleSocket);
            if (email == null) return;
            string password = Worker.CheckRequiredJsonField(ref json, "password", JTokenType.String, ref handleSocket);
            if (password == null) return;
            string confirmPassword = Worker.CheckRequiredJsonField(ref json, "confirm_password", JTokenType.String, ref handleSocket);
            if (confirmPassword == null) return;
            if (!Validator.ValidateEmail(email))
            {
                Logger.WriteLog("Validation email account - false", LogLevel.Usual);
                Worker.JsonAnswer(false, "Validation email - false", ref handleSocket);
            }
            else if (!Validator.ValidatePassword(password))
            {
                Logger.WriteLog("Validation password account - false", LogLevel.Usual);
                Worker.JsonAnswer(false, "Validation password - false", ref handleSocket);
            }
            else if (!Validator.EqualsPasswords(password, confirmPassword))
            {
                Logger.WriteLog("Validation confirm password account - false", LogLevel.Usual);
                Worker.JsonAnswer(false, "Validation confirm password - false", ref handleSocket);
            }
            else if (Database.user.SelectEmail(email) != null)
            {
                Logger.WriteLog("Verification account - false, account is exist now", LogLevel.Usual);
                Worker.JsonAnswer(false, "Verification account - false, account is exist now.", ref handleSocket);
            }
            UserCache user = new UserCache();
            user.user_hash = GenerateRandomHash(100);
            user.user_email = email;
            user.user_password = Validator.HashPassword(password);
            user.created_at = (int)(DateTime.UtcNow - Worker.unixed).TotalSeconds;
            Database.user.Add(user);
            Logger.WriteLog("Registrate user with email =" + email, LogLevel.Usual);
            Worker.mail.SendEmail(email, "Activation account", "Your registration url:\r\nhttp://" + Server.ip + ":" + Server.port + "/UpdateState/?confirm_hash=" + user.user_hash);
            Worker.JsonAnswer(true, "User with email =" + email + " successfully registered. Message sent to email.", ref handleSocket);
        }
        public void Login(ref JObject json, ref Socket handleSocket)
        {
            if (json == null) { return; }
            string email = Worker.CheckRequiredJsonField(ref json, "email", JTokenType.String, ref handleSocket);
            if (email == null) return;
            string password = Worker.CheckRequiredJsonField(ref json, "password", JTokenType.String, ref handleSocket);
            if (password == null) return;
            if (Validator.ValidateEmail(email) == false)
            {
                Logger.WriteLog("Validation email account - false", LogLevel.Usual);
                Worker.JsonAnswer(false, "Validation email account - false", ref handleSocket);
            }
            UserCache user = Database.user.SelectEmail(email);
            if (user == null)
            {
                Logger.WriteLog("Verification account - false, account is not exist now", LogLevel.Usual);
                Worker.JsonAnswer(false, "Account is not exist yet, email -" + email, ref handleSocket);
            }
            else if (user.user_state == 0)
            {
                Logger.WriteLog("Verification account - false, account doens't have active state", LogLevel.Usual);
                Worker.JsonAnswer(false, "Verification account - false, account is not activated", ref handleSocket);
            }
            else if (!Validator.VerifyHashedPassword(user.user_password, password))
            {
                Logger.WriteLog("Verification account - false, wrong password", LogLevel.Usual);
                Worker.JsonAnswer(false, "Verification account - false, wrong password", ref handleSocket);
            }
            else
            {
                Logger.WriteLog("Verification account - true", LogLevel.Usual);
                user.user_password = null;
                user.user_hash = null;
                Worker.JsonData(user, ref handleSocket);
            }
        }
        public void ChangePassword(ref JObject json, ref Socket handleSocket)
        {
            if (json == null) { return; }
            int? user_id = Worker.CheckRequiredJsonField(ref json, "user_id", JTokenType.Integer, ref handleSocket);
            if (user_id == null) return;
            string old_password = Worker.CheckRequiredJsonField(ref json, "old_password", JTokenType.String, ref handleSocket);
            if (old_password == null) return;
            string new_password = Worker.CheckRequiredJsonField(ref json, "new_password", JTokenType.String, ref handleSocket);
            if (new_password == null) return;
            string confirm_new_password = Worker.CheckRequiredJsonField(ref json, "confirm_new_password", JTokenType.String, ref handleSocket);
            if (confirm_new_password == null) return;
            UserCache user = Database.user.SelectId(user_id);
            if (user == null)
            {
                Logger.WriteLog("Verification account - false, can not find id account", LogLevel.Usual);
                Worker.JsonAnswer(false, "Verification account - false, can not find id account", ref handleSocket);
            }
            else if (!Validator.ValidatePassword(new_password))
            {
                Logger.WriteLog("Validation new password account - false", LogLevel.Usual);
                Worker.JsonAnswer(false, "Validation password - false", ref handleSocket);
            }
            else if (!Validator.EqualsPasswords(new_password, confirm_new_password))
            {
                Logger.WriteLog("Validation confirm password account - false", LogLevel.Usual);
                Worker.JsonAnswer(false, "Validation confirm password - false", ref handleSocket);
            }
            else
            {
                Database.user.UpdatePassword(user.user_id, Validator.HashPassword(new_password));
                Worker.JsonAnswer(true, "New password was successfully updated", ref handleSocket);
            }
        }
        public void Recovery(ref string request, ref Socket handleSocket)
        {
            string email = Worker.FindParamFromRequest(ref request, "email", TypeCode.String);
            if (email == null)
            {
                Worker.JsonAnswer(false, "Can not find email address in request params", ref handleSocket);
            }
            if (!Validator.ValidateEmail(email))
            {
                Logger.WriteLog("Validation email account - false", LogLevel.Usual);
                Worker.JsonAnswer(false, "Validation email - false", ref handleSocket);
            }
            UserCache user = Database.user.SelectEmail(email);
            if (user != null)
            {
                string new_password = Validator.GenerateHash();
                Database.user.UpdatePassword(user.user_id, Validator.HashPassword(new_password));
                Worker.mail.SendEmail(email, "Recovery password", "Recovery password. New password:" + new_password);
                Worker.JsonAnswer(true, "Send message to email=" + email + "", ref handleSocket);
            }
            else
            {
                Logger.WriteLog("Verification account - false, account is not exist.", LogLevel.Usual);
                Worker.JsonAnswer(false, "Account is not exist yet, email -" + email, ref handleSocket);
            }
        }
        public void DeleteAccount(ref string request, ref Socket handleSocket)
        {
            int? user_id = Worker.FindParamFromRequest(ref request, "user_id", TypeCode.Int32);
            UserCache user = Database.user.SelectId(user_id);
            if (user == null)
            {
                Logger.WriteLog("Get account by id - false.", LogLevel.Usual);
                Worker.JsonAnswer(false , "Unknown receided id", ref handleSocket);
            }
            List<App> apps = Database.app.SelectByUserId(user_id);
            if (apps != null)
            {
                foreach (App app in apps)
                {
                    Worker.uploader.DeleteDirectory(app.app_hash);
                    Database.file.DeleteById(app.app_id);
                }
            }
            Database.user.Delete(user_id);
            Worker.JsonAnswer(true, "User deleted, user_id->" + user_id, ref handleSocket);
        }

        public void UpdateAccountState(ref string request, ref Socket handleSocket)
        {
            string confirm_hash = Worker.FindParamFromRequest(ref request, "confirm_hash", TypeCode.String);
            if (confirm_hash == null)
            {
                Logger.WriteLog("Can't get confirm hash from request", LogLevel.Error);
                Worker.JsonAnswer(false, "Can't get confirm hash from request", ref handleSocket);
            }
            UserCache user = Database.user.SelectHash(confirm_hash);
            if (user == null)
            {
                Logger.WriteLog("Get account by user_id - false.", LogLevel.Error);
                Worker.JsonAnswer(false, "Unknown receided user_id", ref handleSocket);
            }
            else
            {
                Database.user.UpdateState(user.user_id);
                Logger.WriteLog("User state update, user_id->" + user.user_id, LogLevel.Usual);
                Worker.JsonAnswer(true, "User state was successfully update.", ref handleSocket);
            }
        }
        private string GenerateRandomHash(int length)
        {
            string hash = "";
            for (int i = 0; i < length; i++)
            {
                hash += chars[random.Next(chars.Length)];
            }
            return hash;
        }
    }
}
