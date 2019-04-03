using System;
using System.Text;
using Newtonsoft.Json;
using System.Diagnostics;
using System.Net.Sockets;
using Newtonsoft.Json.Linq;
using Common.Functional.Mail;
using YobiApp.Functional.Upload;
using YobiApp.Functional.FileWork;
using YobiApp.Functional.RegLogin;
using System.Text.RegularExpressions;

namespace Common
{
    public static class Worker
    {
        public static LoaderFile loaderFile;
        public static UploadApp uploader;
        public static Authorization auth;
        public static MailF mail;
        public static UploadF upload;

        public static DateTime unixed = new DateTime(1970, 1, 1, 1, 1, 1, 1);

        public static void Initialization()
        {
            mail = new MailF();
            auth = new Authorization();
            loaderFile = new LoaderFile();
            uploader = new UploadApp();
            upload = new UploadF();
        }
        public static void JsonRequest(ref string json, ref Socket remoteSocket)
        {
            if (string.IsNullOrEmpty(json))
            {
                string response = "HTTP/1.1 200\r\n";
                response += "Version: HTTP/1.1\r\n";
                response += "Content-Type: application/json\r\n";
                response += "Access-Control-Allow-Headers: *\r\n";
                response += "Access-Control-Allow-Origin: *\r\n";
                response += "Content-Length: " + (json.Length).ToString();
                response += "\r\n\r\n";
                response += json;
                if (remoteSocket.Connected)
                {
                    remoteSocket.Send(Encoding.ASCII.GetBytes(response));
                }
                Debug.Write(response);
                Logger.WriteLog("Return http 200 JSON response", LogLevel.Usual);
            }
        }
        public static void ErrorJsonRequest(ref string json, ref Socket remoteSocket)
        {
            if (string.IsNullOrEmpty(json))
            {
                string response = "HTTP/1.1 500\r\n";
                response += "Version: HTTP/1.1\r\n";
                response += "Content-Type: application/json\r\n";
                response += "Access-Control-Allow-Headers: *\r\n";
                response += "Access-Control-Allow-Origin: *\r\n";
                response += "Content-Length: " + json.Length.ToString();
                response += "\r\n\r\n";
                response += json;
                if (remoteSocket.Connected)
                {
                    remoteSocket.Send(Encoding.ASCII.GetBytes(response));
                }
                Debug.WriteLine(response);
                Logger.WriteLog("Return http 500 responce with JSON data.", LogLevel.Usual);
            }
        }
        public static dynamic CheckRequiredJsonField(ref JObject json, string field_name, JTokenType field_type, ref Socket remoteSocket)
        {
            if (json == null)
            {
                Logger.WriteLog("Insert json is null, function CheckRequiredJsonField", LogLevel.Error);
                return null;
            }
            if (json.ContainsKey(field_name))
            {
                JToken token = json.GetValue(field_name);
                if (token.Type == field_type)
                {
                    return token;
                }
                else
                {
                    Logger.WriteLog("Required field is not in correct format, field_name=" + field_name + " field_type=" + field_type, LogLevel.Error);
                    JsonAnswer(false, "Required field is not in correct format, field_name=" + field_name + " field_type=" + field_type, ref remoteSocket);
                    return null;
                }
            }
            else
            {
                Logger.WriteLog("Json does not contain required field, field_name=" + field_name, LogLevel.Error);
                JsonAnswer(false, "Json does not contain required field, field_name=" + field_name, ref remoteSocket);
                return null;
            }
        }
        public static dynamic DefineJsonRequest(ref string request, ref Socket remoteSocket)
        {
            JObject json = GetJsonFromRequest(ref request);
            if (json != null)
            {
                return json;
            }
            else
            {
                JsonAnswer(false, "Server can't define json object from request.", ref remoteSocket);
                Logger.WriteLog("Server can't define json object from request.", LogLevel.Error);
                return null;
            }
        }
        public static dynamic GetJsonFromRequest(ref string request)
        {
            if (!string.IsNullOrEmpty(request))
            {
                string json = "";
                int searchIndex = request.IndexOf("application/json", StringComparison.Ordinal);
                if (searchIndex == -1)
                {
                    Logger.WriteLog("Can not find \"application/json\" in request.", LogLevel.Error);
                    return null;
                }
                int indexFirstChar = request.IndexOf("{", searchIndex, StringComparison.Ordinal);
                if (indexFirstChar == -1)
                {
                    Logger.WriteLog("Can not find start json in request.", LogLevel.Error);
                    return null;
                }
                int indexLastChar = request.LastIndexOf("}", StringComparison.Ordinal);
                if (indexLastChar == -1)
                {
                    Logger.WriteLog("Can not find end json in request.", LogLevel.Error);
                    return null;
                }
                try
                {
                    json = request.Substring(indexFirstChar, indexLastChar - indexFirstChar + 1);
                    return JsonConvert.DeserializeObject<dynamic>(json);
                }
                catch (Exception e)
                {
                    Logger.WriteLog("Can not define json object in request. function GetJsonFromRequest(). Message: " + e.Message, LogLevel.Error);
                    return null;
                }
            }
            else
            {
                Logger.WriteLog("Insert request is null or empty, function GetJsonFromRequest", LogLevel.Error);
                return null;
            }
        }
        public static string FindValueContentDisposition(ref string request, string key)
        {
            if (string.IsNullOrEmpty(request))
            {
                Logger.WriteLog("Input value is null or empty, function FindValueContentDisposition()", LogLevel.Error);
                return null;
            }
            string findKey = "Content-Disposition: form-data; name=\"" + key + "\"";
            string boundary = GetBoundaryRequest(ref request);
            if (string.IsNullOrEmpty(boundary))
            {
                Logger.WriteLog("Can not get boundary from request, function FindValueContentDisposition", LogLevel.Error);
                return null;
            }
            boundary = "\r\n--" + boundary;
            if (request.Contains(findKey))
            {
                int searchKey = request.IndexOf(findKey, StringComparison.Ordinal) + findKey.Length + "\r\n\r\n".Length;
                if (searchKey == -1)
                {
                    Logger.WriteLog("Can not find content-disposition key from request, function FindValueContentDisposition", LogLevel.Error);
                    return null;
                }
                int transfer = request.IndexOf(boundary, searchKey, StringComparison.Ordinal);
                if (transfer == -1)
                {
                    Logger.WriteLog("Can not end boundary from request, function FindValueContentDisposition", LogLevel.Error);
                    return null;
                }
                try
                {
                    return request.Substring(searchKey, transfer - searchKey);
                }
                catch
                {
                    Logger.WriteLog("Can not define key value from request, function FindValueContentDisposition", LogLevel.Error);
                    return null;
                }
            }
            else
            {
                Logger.WriteLog("Request does not contain find key, function FindValueContentDisposition", LogLevel.Error);
                return null;
            }
        }
        public static dynamic GetFormDataField(ref string request, ref Socket remoteSocket, string field_name, TypeCode field_type)
        {
            if (string.IsNullOrEmpty(request))
            {
                Logger.WriteLog("Input value is null or empty, function GetFormDataField()", LogLevel.Error);
                return null;
            }
            string field_value = FindValueContentDisposition(ref request, field_name);
            if (field_value != null)
            {
                switch (field_type)
                {
                    case TypeCode.Int32:
                        int? field_int_value = ConvertSaveString(ref field_value, field_type);
                        if (field_int_value != null)
                        {
                            return field_int_value;
                        }
                        else
                        {
                            JsonAnswer(false, "Can not define field_type of value", ref remoteSocket);
                            return null;
                        }
                    case TypeCode.String: return field_value;
                    default:
                        JsonAnswer(false, "Can not define field_type of value", ref remoteSocket);
                        Logger.WriteLog("Can not define field_type of value, function CheckFormDataField", LogLevel.Error);
                        return null;
                }
            }
            else
            {
                JsonAnswer(false, "Can't define form-data value from request",ref remoteSocket);
                Logger.WriteLog("Can't define form-data value from request", LogLevel.Error);
                return null;
            }
        }
        public static dynamic FindParamFromRequest(ref string request, string key, TypeCode field_type)
        {
            if (string.IsNullOrEmpty(request))
            {
                Logger.WriteLog("Input value is null or empty, function FindParamFromRequest()", LogLevel.Error);
                return null;
            }
            Regex urlParams = new Regex(@"[\?&](" + key + @"=([^&=#\s]*))", RegexOptions.Multiline);
            Match match = urlParams.Match(request);
            if (match.Success)
            {
                string field_value = match.Value;
                field_value = field_value.Substring(key.Length + 2);
                switch (field_type)
                {
                    case TypeCode.Int32:
                        int? field_int_value = ConvertSaveString(ref field_value, field_type);
                        if (field_int_value != null)
                        {
                            return field_int_value;
                        }
                        else
                        {
                            Logger.WriteLog("Can not define field_type of value, function FindParamFromRequest()", LogLevel.Error);
                            return null;
                        }
                    case TypeCode.String: return field_value;
                    default:
                        Logger.WriteLog("Can't define field_type of value, function FindParamFromRequest()", LogLevel.Error);
                        return null;
                }
            }
            else
            {
                Logger.WriteLog("Can not define url parameter from request, function FindParamFromRequest", LogLevel.Error);
                return null;
            }
        }
        private static void HttpIternalServerError(ref Socket remoteSocket)
        {
            string response = "";
            string responseBody = "<HTML>" +
                                 "<BODY>" +
                                 "<h1> 500 Internal Server Error...</h1>" +
                                 "</BODY></HTML>";
            response = "HTTP/1.1 500 \r\n" +
                       "Version: HTTP/1.1\r\n" +
                       "Content-Type: text/html; charset=utf-8\r\n" +
                       "Content-Length: " + (response.Length + responseBody.Length) +
                       "\r\n\r\n" +
                       responseBody;
            if (remoteSocket.Connected)
            {
                remoteSocket.Send(Encoding.ASCII.GetBytes(response));
            }
            Logger.WriteLog("HTTP 500 Error link response", LogLevel.Error);
        }
        public static dynamic ConvertSaveString(ref string value, TypeCode value_type)
        {
            if (string.IsNullOrEmpty(value))
            {
                Logger.WriteLog("Value is null or empty, function ConvertSaveString", LogLevel.Error);
                return null;
            }
            try
            {
                switch (value_type)
                {
                    case TypeCode.Int32: return Convert.ToInt32(value);
                    case TypeCode.Double: return Convert.ToDouble(value);
                    default:
                        Logger.WriteLog("Can not define type of value, function ConvertSaveString()", LogLevel.Error);
                        return null;
                }
            }
            catch
            {
                Logger.WriteLog("Can not convert current value to type->" + value_type + ", function ConvertSaveString", LogLevel.Error);
                return null;
            }
        }
        public static string GetBoundaryRequest(ref string request)
        {
            int i = 0;
            bool exist = false;
            string boundary = "";
            string subRequest = "";
            int first = request.IndexOf("boundary=", StringComparison.Ordinal);
            if (first == -1)
            {
                Logger.WriteLog("Can not search boundary from request", LogLevel.Error);
                return null;
            }
            first += 9;                                     // boundary=.Length
            if (request.Length > 2500 + first)
            {
                subRequest = request.Substring(first, 2000);
            }
            else
            {
                subRequest = request.Substring(first);
            }
            while (!exist)
            {
                if (subRequest[i] == '\r')
                {
                    exist = true;
                }
                else
                {
                    boundary += subRequest[i];
                    i++;
                }
                if (i > 2000)
                {
                    Logger.WriteLog("Can not define end of boundary request", LogLevel.Error);
                    return null;
                }
            }
            return boundary;
        }
        public static string JsonData(dynamic data, ref Socket remoteSocket)
        {
            string jsonAnswer = "{\r\n" +
                "\"success\":true,\r\n" +
                "\"data\":" + JsonConvert.SerializeObject(data) + "\r\n" +
                "}";
            JsonRequest(ref jsonAnswer, ref remoteSocket);
            return jsonAnswer;
        }
        public static void JsonAnswer(bool success, string message, ref Socket remoteSocket)
        {
            string jsonAnswer = "{\r\n \"success\":" + success.ToString().ToLower() + ",\r\n" +
                "\"message\":\"" + message + "\"\r\n" +
                "}";
            if (success)
            {
                JsonRequest(ref jsonAnswer, ref remoteSocket);
            }
            else
            {
                ErrorJsonRequest(ref jsonAnswer, ref remoteSocket);
            }
        }
    }
}