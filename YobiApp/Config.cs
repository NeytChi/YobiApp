using System;
using System.IO;
using System.Diagnostics;
using Newtonsoft.Json.Linq;

namespace Common
{
    public static class Config
    {
        public static JObject JsonObject;
        public static string fileName = "conf";
        public static string IP = "127.0.0.1";
        public static string Domen = "(none)";
        public static int Port = 8023;
        public static string currentDirectory = Directory.GetCurrentDirectory();       // Return of the path occurs without the last '/' (pointer to the directory)
        public static bool initiated = false;

        private static JObject GetConfigJson(string info)
        {
            JObject json = JObject.Parse(info);
            if (json.ContainsKey("ip") && json.ContainsKey("port") && json.ContainsKey("domen"))
            {
                return json;
            }
            else
            {
                Console.WriteLine("Can not get JsonObject, json doens't have set values");
                return null;
            }
        }
        private static string ReadConfigJsonData()
        {
            if (File.Exists(fileName))
            {
                using (var fstream = File.OpenRead(fileName))
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
                Console.WriteLine("Can not read file=" + fileName + " , function Config.ReadConfigJsonData()");
                return string.Empty;
            }
        }
        public static dynamic GetConfigValue(string conf_name, TypeCode type_value)
        {
            if (!initiated)
            {
                Initialization();
            }
            if (JsonObject != null)
            {
                if (JsonObject.ContainsKey(conf_name))
                {
                    JToken token = JsonObject.GetValue(conf_name);
                    switch (type_value)
                    {
                        case TypeCode.Int32:
                            if (token.Type == JTokenType.Integer)
                            {
                                return JsonObject.GetValue(conf_name);
                            }
                            else
                            {
                                Console.WriteLine("Can't get value, type of value don't equals to insert type, function GetConfigValue");
                                return null;
                            }
                        case TypeCode.String:
                            if (token.Type == JTokenType.String)
                            {
                                return JsonObject.GetValue(conf_name);
                            }
                            else
                            {
                                Console.WriteLine("Can't get value, type of value don't equals to insert type, function GetConfigValue");
                                return null;
                            }
                        default:
                            Console.WriteLine("Can't get value, type of value not define, function GetConfigValue");
                            return null;
                    }
                }
                else
                {
                    Console.WriteLine("Can not get value, json doesn't have this value, value=" + conf_name + ", function GetConfigValue");
                    return null;
                }
            }
            else
            {
                Console.WriteLine("Can not get value, Json Object did not create, function GetConfigValue");
                return null;
            }
        }
        public static void Initialization()
        {
            initiated = true;
            FileInfo fileExist = new FileInfo(currentDirectory + "/" + fileName);
            if (fileExist.Exists)
            {
                string infoJson = ReadConfigJsonData();
                JsonObject = GetConfigJson(infoJson);
                if (JsonObject != null)
                {
                    Port = GetConfigValue("port", TypeCode.Int32);
                    IP = GetConfigValue("ip", TypeCode.String);
                    Domen = GetConfigValue("domen", TypeCode.String);
                }
                else
                {
                    Debug.WriteLine("Start with default config setting.");
                }
            }
            else
            {
                Debug.WriteLine("Start with default config setting.");
            }
        }
    }
}