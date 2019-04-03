using System;
using System.Net;
using System.Text;
using System.Threading;
using System.Net.Sockets;
using System.Diagnostics;
using Newtonsoft.Json.Linq;
using YobiApp.Functional.Tasker;
using System.Text.RegularExpressions;

namespace Common
{
    public static class Server
    {
        public static int port = 8023;
        public static string ip = "127.0.0.1";
        public static string domen = "(none)";
        private static Socket socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
        private static Regex contentlength = new Regex("ength: [0-9]*", RegexOptions.Compiled);
        private static readonly string[] methods =
        {
            "GET",
            "POST",
            "OPTIONS"
        };
        public static void InitListenSocket()
        {
            IPEndPoint iPEndPoint = new IPEndPoint(IPAddress.Parse(ip), port);
            socket.Bind(iPEndPoint);
            socket.Listen(1000);
            Logger.WriteLog("Server run. Host_Port=" + ip + ":" + port, LogLevel.Usual);
            while (true)
            {
                Socket handleSocket = socket.Accept();
                Thread thread = new Thread(() => ReceivedSocketData(ref handleSocket))
                {
                    IsBackground = true
                };
                thread.Start();
            }
        }
        private static void ReceivedSocketData(ref Socket handleSocket)
        {
            byte[] buffer = new byte[1096];
            int bytes = 0;
            string request = "";
            int ContentLength = 0;
            for (; ; )
            {
                if (buffer.Length < bytes + 300)
                {
                    Array.Resize(ref buffer, bytes + 2000);
                }
                else
                {
                    bytes += handleSocket.Receive(buffer, bytes, 60, SocketFlags.None);
                }
                if (bytes > 500 && bytes < 1000 && buffer.Length == 1096)
                {
                    request = Encoding.ASCII.GetString(buffer, 0, bytes);
                    if (request.Contains("content-length:") || request.Contains("Content-Length:"))
                    {
                        ContentLength = GetRequestContentLenght(ref request);
                        if (ContentLength > 0 && ContentLength < 210000000)
                        {
                            Array.Resize(ref buffer, ContentLength + bytes);
                        }
                        else if (ContentLength > 210000000) handleSocket.Close();
                    }
                }
                if (handleSocket.Available == 0 && bytes >= ContentLength) { break; }
                if (handleSocket.Available == 0 && bytes < ContentLength)
                {
                    if ((handleSocket.Poll(10000, SelectMode.SelectRead) && (handleSocket.Available == 0)) || !handleSocket.Connected)
                    {
                        handleSocket.Close();
                        Logger.WriteLog("Remote socket was disconnected.", LogLevel.Usual);
                        break;
                    }
                }
                if (bytes > 210000000)
                {
                    HttpIternalServerError(ref handleSocket);
                    handleSocket.Close();
                    break;
                }
            }
            if (handleSocket.Connected)
            {
                request = Encoding.ASCII.GetString(buffer, 0, bytes);
                IdentifyRequest(ref request, ref handleSocket, ref buffer, ref bytes);
            }
            if (handleSocket.Connected) { handleSocket.Close(); }
        }
        private static void IdentifyRequest(ref string request,ref Socket handleSocket,ref byte[] buffer,ref int bytes)
        {
            Debug.WriteLine("Request:");
            Debug.WriteLine(request);
            switch (GetMethodRequest(ref request))
            {
                case "GET":
                    HandleGetRequest(ref request,ref handleSocket);
                    break;
                case "POST":
                    JObject json = null;
                    switch (FindURLRequest(ref request, "POST").ToLower())
                    {
                        case "registration":
                            json = Worker.GetJsonFromRequest(ref request);
                            Worker.auth.Registration(ref json, ref handleSocket);
                            break;
                        case "login":
                            json = Worker.GetJsonFromRequest(ref request);
                            Worker.auth.Login(ref json, ref handleSocket);
                            break;
                        case "recovery":
                            Worker.auth.Recovery(ref request, ref handleSocket);
                            break;
                        case "newpassword": case "newpass":
                            json = Worker.GetJsonFromRequest(ref request);
                            Worker.auth.ChangePassword(ref json, ref handleSocket);
                            break;
                        case "account/delete":
                            Worker.auth.DeleteAccount(ref request, ref handleSocket);
                            break;
                        case "upload":
                            json = Worker.GetJsonFromRequest(ref request);
                            Worker.upload.UploadBuild(ref request, ref buffer, ref bytes, ref handleSocket);
                            break;
                        case "app":
                            Worker.upload.SelectApp(ref request, ref handleSocket);
                            break;
                        case "app/select_all":
                            Worker.upload.SelectMassApps(ref request, ref handleSocket);
                            break;
                        case "app/delete":
                            Worker.upload.DeleteApp(ref request, ref handleSocket);
                            break;
                        default:
                            HttpErrorUrl(ref handleSocket);
                            break;
                    }
                    break;
                case "OPTIONS": HttpOptions(ref handleSocket);
                    break;
                default: HttpErrorUrl(ref handleSocket);
                    break;
            }
        }
        public static void HandleGetRequest(ref string request,ref Socket handleSocket)
        {
            switch (FindURLRequest(ref request, "GET").ToLower())
            {
                case "logs":
                    HttpLogs(ref handleSocket);
                    break;
                case "UpdateState":
                    Worker.auth.UpdateAccountState(ref request, ref handleSocket);
                    break;
                default:
                    HttpErrorUrl(ref handleSocket);
                    break;
            }
        }
        /// <summary>
        /// Finds the URL in request.
        /// </summary>
        /// <returns>The URLR equest.</returns>
        /// <param name="request">Request.</param>
        /// <param name="method">Method.</param>
        public static string FindURLRequest(ref string request, string method)
        {
            string url = GetBetween(ref request, method + " ", " HTTP/1.1");
            if (string.IsNullOrEmpty(url)) { return null; }
            int questionUrl = url.IndexOf('?', 1);
            if (questionUrl == -1)
            {
                url = url.Substring(1);
                if (url[url.Length - 1] != '/')
                {
                    return url.ToLower();                                       // handle this pattern url -> /User || /User/Profile
                }
                else
                {
                    return url.Remove(url.Length - 1).ToLower();                // handle this pattern url -> /User/ || /User/Profile/
                }
            }
            else
            {
                if (url[questionUrl - 1] == '/')                                // handle this pattern url -> /User/Profile/?id=1111 -> /User/Profile/
                {
                    return url.Substring(1, questionUrl - 2).ToLower();         // handle this pattern url -> User/Profile - return
                }
                else
                {
                    Logger.WriteLog("Can not define pattern of url, function FindURLRequest()", LogLevel.Error);
                    return null;                                                // Don't handle this pattern url -> /User?id=1111 and /User/Profile?id=9999 
                }
            }
        }
        public static string GetMethodRequest(ref string request)
        {
            if (!string.IsNullOrEmpty(request))
            {
                if (request.Length < 20)
                {
                    Logger.WriteLog("Can not define method of request, input request have not enough characters, function GetMethodRequest()", LogLevel.Error);
                    return null;
                }
                string requestMethod = request.Substring(0, 20);
                for (int i = 0; i < methods.Length; i++)
                {
                    if (requestMethod.Contains(methods[i]))
                    {
                        string method = request.Substring(0, methods[i].Length);
                        if (method == methods[i])
                        {
                            return method;
                        }
                        else
                        {
                            Logger.WriteLog("Can not define method of request, function GetMethodRequest()", LogLevel.Error);
                            return null;
                        }
                    }
                }
                Logger.WriteLog("Can not define method of request, request does not contains available methods, function GetMethodRequest()", LogLevel.Error);
                return null;
            }
            else
            {
                Logger.WriteLog("Input request is null or empty, function GetMethodRequest", LogLevel.Error);
                return null;
            }
        }
        public static string GetBetween(ref string source, string start, string end)
        {
            if (!string.IsNullOrEmpty(source))
            {
                if (source.Contains(start) && source.Contains(end))
                {
                    int Start = source.IndexOf(start, 0, StringComparison.Ordinal) + start.Length;
                    if (Start == -1)
                    {
                        Logger.WriteLog("Can not find start of source, function GetBetween()", LogLevel.Error);
                        return null;
                    }
                    int End = source.IndexOf(end, Start, StringComparison.Ordinal);
                    if (End == -1)
                    {
                        Logger.WriteLog("Can not find end of source, function GetBetween()", LogLevel.Error);
                        return null;
                    }
                    return source.Substring(Start, End - Start);
                }
                else
                {
                    Logger.WriteLog("Source does not contains search values, function GetBetween()", LogLevel.Error);
                    return null;
                }
            }
            else
            {
                Logger.WriteLog("Source is null or empty, function GetBetween()", LogLevel.Error);
                return null;
            }
        }
        private static void HttpOptions(ref Socket remoteSocket)
        {
            string response = "HTTP/1.1 200 OK\r\n" +
                              "Access-Control-Allow-Methods: POST, GET, OPTIONS\r\n" +
                              "Access-Control-Allow-Headers: *\r\n" +
                              "Access-Control-Allow-Origin: *\r\n" +
                              "Vary: Accept-Encoding, Origin\r\n" +
                              "Content-Encoding: gzip\r\n" +
                              "Content-Length: 0\r\n" +
                              "Keep-Alive: timeout=300\r\n" +
                              "Connection: Keep-Alive\r\n" +
                              "Content-Type: multipart/form-data\r\n\r\n";
            if (remoteSocket.Connected)
            {
                remoteSocket.Send(Encoding.ASCII.GetBytes(response));
            }
            Logger.WriteLog("HTTP Response " + ip + ":" + port.ToString() + " - OPTIONS", LogLevel.Usual);
        }
        private static void HttpLogs(ref Socket remoteSocket)
        {
            string response = "";
            byte[] logs = Logger.ReadMassiveLogs();
            string start = "<HTML><BODY>Logs massive information:<br><hr>";
            string end = "</BODY></HTML>";
            response += "HTTP/1.1 200 OK\r\n";
            response += "Version: HTTP/1.1\r\n";
            response += "Content-Type: text/html; charset=utf-8\r\n";
            response += "Content-Length: " + (start.Length + end.Length + logs.Length);
            response += "\r\n\r\n";
            response += start;
            byte[] answerstart = Encoding.ASCII.GetBytes(response);
            byte[] answerend = Encoding.ASCII.GetBytes(end);
            byte[] requestbyte = new byte[logs.Length + answerstart.Length + answerend.Length];
            answerstart.CopyTo(requestbyte, 0);
            logs.CopyTo(requestbyte, answerstart.Length);
            answerend.CopyTo(requestbyte, answerstart.Length + logs.Length);
            remoteSocket.Send(requestbyte);
            GC.Collect();
            Logger.WriteLog("HTTP Response 127.0.0.1:8000/LogInfo/", LogLevel.Usual);
        }
        private static void HttpIternalServerError(ref Socket remoteSocket)
        {
            string response = "";
            string responseBody = "";
            responseBody = string.Format("<HTML>" +
                                         "<BODY>" +
                                         "<h1> 500 Internal Server Error !..</h1>" +
                                         "</BODY></HTML>");
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
            Debug.Write(response);
            Logger.WriteLog("HTTP 400 Error link response", LogLevel.Error);
        }
        private static void HttpErrorUrl(ref Socket remoteSocket)
        {
            string responseBody = string.Format("<HTML><BODY><h1>error url...</h1></BODY></HTML>");
            string response = "HTTP/1.1 400 \r\n";
            response += "Version: HTTP/1.1\r\n";
            response += "Content-Type: text/html; charset=utf-8\r\n";
            response += "Content-Length: " + (responseBody.Length);
            response += "\r\n\r\n";
            response += responseBody;
            if (remoteSocket.Connected)
            {
                remoteSocket.Send(Encoding.ASCII.GetBytes(response));
            }
            Logger.WriteLog("HTTP 400 Error link response", LogLevel.Error);
        }
        /// <summary>
        /// Gets value "content lenght" from request.
        /// </summary>
        /// <returns>The request content lenght.</returns>
        /// <param name="request">Picie of request.</param>
        public static int GetRequestContentLenght(ref string request)
        {
            try
            {
                Match resultContentLength = contentlength.Match(request);
                if (resultContentLength.Success)
                {
                    return Convert.ToInt32(resultContentLength.Value.Substring("ength: ".Length)) + resultContentLength.Index + resultContentLength.Length;
                }
                else
                {
                    return 0;
                }
            }
            catch
            {
                Logger.WriteLog("Error function GetRequestContentLenght(), exception with converting to int value", LogLevel.Error);
                return 0;
            }
        }
        public static void Dispose()
        {
            Logger.Dispose();
            socket.Close();
            Database.connection.Close();
        }
    }
}

