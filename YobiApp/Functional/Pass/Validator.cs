using System;
using System.ComponentModel.DataAnnotations;
using System.Security.Cryptography;
using Common;

namespace YobiApp.Functional.Pass
{
    public class Validator
    {
        private const int MIN_LENGTH = 6;
        private const int MAX_LENGTH = 20;
        private EmailAddressAttribute foo = new EmailAddressAttribute();
        private Random random = new Random();
        private string Alphavite = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789";

        public bool ValidateEmail(string email)
        {
            bool bar = foo.IsValid(email);
            string log = string.Format("Validating email {0} address = {1}.", email, bar);
            Logger.WriteLog(log, LogLevel.Usual);
            if (bar == true)
            {
                return true;
            }
            else return false;
        }
        public bool ValidatePassword(string password)
        {
            if (password == null || password == "") 
            { 
                return false; 
            }
            bool meetsLengthRequirements = password.Length >= MIN_LENGTH && password.Length <= MAX_LENGTH;
            //bool hasUpperCaseLetter = false;
            bool hasLowerCaseLetter = false;
            //bool hasDecimalDigit = false;

            if (meetsLengthRequirements)
            {
                foreach (char c in password)
                {
                    //if (char.IsUpper(c)) hasUpperCaseLetter = true;
                    if (char.IsLower(c)) hasLowerCaseLetter = true;
                    //else if (char.IsDigit(c)) hasDecimalDigit = true;
                }
            }
            bool isValid = meetsLengthRequirements
                //&& hasUpperCaseLetter
                && hasLowerCaseLetter;
                //&& hasDecimalDigit;
            Logger.WriteLog("Validating password", LogLevel.Usual);
            return isValid;
        }
        public bool EqualsPasswords(string password, string confirmpassword)
        {
            if (password == confirmpassword)
            {
                Logger.WriteLog("Validating confirm password = true", LogLevel.Usual);
                return true;
            }
            else
            {
                Logger.WriteLog("Validating confirm password = false", LogLevel.Usual);
                return false;
            }
        }
        public string GenerateHash()
        {
            string hash = "";
            for (int i = 0; i < 6; i++)
            {
                hash += Alphavite[random.Next(Alphavite.Length)];
            }
            return hash;
        }
        public string HashPassword(string password)
        {
            byte[] salt;
            byte[] buffer2;
            if (password == null)
            {
                return "";
            }
            using (Rfc2898DeriveBytes bytes = new Rfc2898DeriveBytes(password, 0x10, 0x3e8))
            {
                salt = bytes.Salt;
                buffer2 = bytes.GetBytes(0x20);
            }
            byte[] dst = new byte[0x31];
            Buffer.BlockCopy(salt, 0, dst, 1, 0x10);
            Buffer.BlockCopy(buffer2, 0, dst, 0x11, 0x20);
            return Convert.ToBase64String(dst);
        }
        public bool VerifyHashedPassword(string hashedPassword, string password)
        {
            byte[] buffer4;
            if (hashedPassword == null)
            {
                return false;
            }
            if (password == null)
            {
                return false;
            }
            byte[] src = Convert.FromBase64String(hashedPassword);
            if ((src.Length != 0x31) || (src[0] != 0))
            {
                return false;
            }
            byte[] dst = new byte[0x10];
            Buffer.BlockCopy(src, 1, dst, 0, 0x10);
            byte[] buffer3 = new byte[0x20];
            Buffer.BlockCopy(src, 0x11, buffer3, 0, 0x20);
            using (Rfc2898DeriveBytes bytes = new Rfc2898DeriveBytes(password, dst, 0x3e8))
            {
                buffer4 = bytes.GetBytes(0x20);
            }
            return ByteArraysEqual(buffer3, buffer4);
        }
        private static bool ByteArraysEqual(byte[] b1, byte[] b2)
        {
            if (b1 == b2) return true;
            if (b1 == null || b2 == null) return false;
            if (b1.Length != b2.Length) return false;
            for (int i = 0; i < b1.Length; i++)
            {
                if (b1[i] != b2[i]) return false;
            }
            return true;
        }
    }
}
