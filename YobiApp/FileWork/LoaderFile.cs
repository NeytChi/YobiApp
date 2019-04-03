using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using Common;
using Common.NDatabase.FileData;

namespace YobiApp.Functional.FileWork
{
	public class LoaderFile 
    {
        private static Random random = new Random();
        public static string CurrentDirectory = Directory.GetCurrentDirectory();
        public static string PathToFiles = "/Files/";
        private static Regex ContentDispositionPattern = new Regex("Content-Disposition: form-data;" +
                                                            " name=\"(.*)\"; filename=\"(.*)\"\r\n" +
                                                            "Content-Type: (.*)\r\n\r\n", RegexOptions.Compiled);
        private static string[] availableExtentions = { "image", "video", "audio", "application" };
        public static string DailyDirectory;

        public LoaderFile() 
		{
			Directory.CreateDirectory(CurrentDirectory + PathToFiles + "Upload/");
		}
        /// <summary>
        /// Multis the loading files. Get files from ascii request, create files in common folder and get all information about this files.
        /// </summary>
        /// <returns>The loading.</returns>
        /// <param name="AsciiRequest">ASCII request.</param>
        /// <param name="buffer">Buffer.</param>
        /// <param name="bytes">Bytes.</param>
        /// <param name="count_files">Count files.</param>
        public static List<FileD> LoadingFiles(ref string AsciiRequest, ref byte[] buffer, ref int bytes, ref int count_files)
        {
            bool endRequest = false;
            int last_position = 0;
            List<Match> dispositionsAscii = new List<Match>();
            List<Match> boundariesAscii = new List<Match>();
            List<FileD> files = new List<FileD>();
            if (CheckHeadersFileRequest(ref AsciiRequest))
            {
                string EndBoundaryAscii = "--" + GetBoundary(ref AsciiRequest);
                Regex endBoundaryPattern = new Regex(EndBoundaryAscii);
                while (!endRequest)
                {
                    Match contentFile = ContentDispositionPattern.Match(AsciiRequest, last_position);
                    if (contentFile.Success && boundariesAscii.Count < count_files)
                    {
                        last_position = contentFile.Index + contentFile.Length;
                        Match boundary = endBoundaryPattern.Match(AsciiRequest, last_position);
                        if (boundary.Success)
                        {
                            dispositionsAscii.Add(contentFile);
                            boundariesAscii.Add(boundary);
                        }
                    }
                    else
                    {
                        endRequest = true;
                    }
                }
                for (int i = 0; i < dispositionsAscii.Count; i++)
                {
                    Match disposition = dispositionsAscii[i];
                    Match boundaries = boundariesAscii[i];
                    byte[] fileBuffer = GetFileBufferByPositions(ref buffer, ref AsciiRequest, ref disposition, ref boundaries);
                    if (fileBuffer != null)
                    {
                        FileD file = CreateFileByInfo(ref disposition);
                        CreateFileBinary(ref file.file_name, ref file.file_path, ref fileBuffer);
                        files.Add(file);
                    }
                    else
                    {
                        Logger.WriteLog("Can not create file from request, file_count=" + i, LogLevel.Error);
                    }
                }
            }
            else
            {
                Logger.WriteLog("Request doesnot has required request fields.", LogLevel.Error);
                return null;
            }
            Logger.WriteLog("Get files from request. From request loaded " + files.Count + " file(s).", LogLevel.Error);
            return files;
        }
        private static byte[] GetFileBufferByPositions(ref byte[] buffer, ref string AsciiRequest, ref Match start, ref Match end)
        {
            try
            {
                byte[] binRequestPart = Encoding.ASCII.GetBytes(AsciiRequest.Substring(0, start.Index + start.Length));
                byte[] binBoundary = Encoding.ASCII.GetBytes(AsciiRequest.Substring(start.Index + start.Length, end.Index - start.Index - start.Length));
                int fileLength = end.Index - start.Index - start.Length;
                byte[] binFile = new byte[fileLength];
                Array.Copy(buffer, binRequestPart.Length, binFile, 0, fileLength);
                return binFile;
            }
            catch (Exception e)
            {
                Debug.WriteLine(e.Message);
                Logger.WriteLog(e.Message, LogLevel.Error);
                return null;
            }
        }
        public static bool CheckHeadersFileRequest(ref string request)
        {
            if (string.IsNullOrEmpty(request))
            {
                Logger.WriteLog("Input value is null or empty, function CheckHeadersFileRequest()", LogLevel.Error);
                return false;
            }
            if (request.Contains("Content-Type: multipart/form-data") || request.Contains("content-type: multipart/form-data"))
            {
                if (request.Contains("boundary="))
                {
                    if (request.Contains("Connection: keep-alive") || request.Contains("connection: keep-alive"))
                    {
                        return true;
                    }
                    else
                    {
                        Logger.WriteLog("Can not find (connection: keep-alive) in request, function CheckHeadersFileRequest", LogLevel.Error);
                        return false;
                    }
                }
                else
                {
                    Logger.WriteLog("Can not find (boundary=) in request, function CheckHeadersFileRequest", LogLevel.Error);
                    return false;
                }
            }
            else
            {
                Logger.WriteLog("Can not find (Content-Type: multipart/form-data) in request, function CheckHeadersFileRequest", LogLevel.Error);
                return false;
            }
        }
        public static FileD CreateFileByInfo(ref Match disposition)
        {
            if (disposition == null)
            {
                Logger.WriteLog("Input value is null, function CreateFileByInfo()", LogLevel.Error);
                return null;
            }
            string ContentType = GetContentType(disposition.Value);
            FileD file = new FileD();
            file.file_extension = GetFileExtention(ContentType);
            file.file_type = GetFileType(disposition.Value);
            file.file_last_name = GetFileName(disposition.Value);
            file.file_name = random.Next(0, 2146567890).ToString();
            file.file_path = GetPathFromExtention(file.file_extension);
            Logger.WriteLog("Create file info by disposition.", LogLevel.Usual);
            return file;
        }
        public static string GetPathFromExtention(string extention)
        {
            switch (extention)
            {
                case "image":
                    Directory.CreateDirectory(CurrentDirectory + PathToFiles + DailyDirectory + "Images/");
                    return PathToFiles + DailyDirectory + "Images/";
                case "video":
                    Directory.CreateDirectory(CurrentDirectory + PathToFiles + DailyDirectory + "Videos/");
                    return PathToFiles + DailyDirectory + "Videos/";
                case "audio":
                    Directory.CreateDirectory(CurrentDirectory + PathToFiles + DailyDirectory + "Audios/");
                    return PathToFiles + DailyDirectory + "Audios/";
                case "application":
                    Directory.CreateDirectory(CurrentDirectory + PathToFiles + DailyDirectory + "Uploads/");
                    return PathToFiles + DailyDirectory + "Uploads/";
                default:
                    return PathToFiles;
            }
        }
        public static string GetFileExtention(string disposition)
        {
            int position;
            for (int i = 0; i < availableExtentions.Length; i++)
            {
                if (disposition.Contains(availableExtentions[i]))
                {
                    position = disposition.IndexOf(availableExtentions[i] + "/", StringComparison.Ordinal);
                    if (position != -1)
                    {
                        position = (availableExtentions[i] + "/").Length;
                        Logger.WriteLog("Get file extention.", LogLevel.Usual);
                        return disposition.Substring(position);
                    }
                }
            }
            Logger.WriteLog("Can not get type of file disposition, function GetFileExtention()", LogLevel.Error);
            return null;
        }
        public static string GetFileType(string disposition)
        {
            int position;
            for (int i = 0; i < availableExtentions.Length; i++)
            {
                if (disposition.Contains(availableExtentions[i]))
                {
                    position = disposition.IndexOf(availableExtentions[i] + "/", StringComparison.Ordinal);
                    if (position != -1)
                    {
                        Logger.WriteLog("Get file type.", LogLevel.Usual);
                        return availableExtentions[i];
                    }
                }
            }
            Logger.WriteLog("Can not get type of file disposition, function GetFileType()", LogLevel.Error);
            return null;
        }
        public static string GetFileName(string disposition)
        {
            int first, end;
            first = disposition.IndexOf("filename=\"", StringComparison.Ordinal);
            if (first == -1)
            {
                Logger.WriteLog("Can not get start of name file, function GetFileName()", LogLevel.Error);
                return null;
            }
            first += "filename=\"".Length;
            end = disposition.IndexOf("\"", first, StringComparison.Ordinal);
            if (end == -1)
            {
                Logger.WriteLog("Can not get end of name file, function GetFileName()", LogLevel.Error);
                return null;
            }
            string filename = disposition.Substring(first, (end - first));
            Logger.WriteLog("Get file name from disposition request", LogLevel.Error);
            return filename;
        }
        public static string GetContentType(string disposition)
        {
            int i = 0;
            bool exist = false;
            string contentType = "";
            int first = (disposition.IndexOf("Content-Type: ", StringComparison.Ordinal));
            if (first == -1)
            {
                Logger.WriteLog("Can not find (Content-Type) start in disposition request, function GetContentType()", LogLevel.Error);
                return null;
            }
            first += "Content-Type: ".Length;
            string subRequest = disposition.Substring(first);
            while (!exist)
            {
                if (subRequest[i] == '\r')
                {
                    exist = true;
                }
                else
                {
                    contentType += subRequest[i];
                    i++;
                }
                if (i > 500)
                {
                    Logger.WriteLog("Can not find (Content-Type) end in disposition request, function GetContentType()", LogLevel.Error);
                    return null;
                }
            }
            return contentType;
        }
        public static bool CreateFileBinary(ref string fileName, ref string pathToSave, ref byte[] byteArray)
        {
            try
            {
                using (Stream fileStream = new FileStream(CurrentDirectory + pathToSave + fileName, FileMode.Create, FileAccess.Write))
                {
                    fileStream.Write(byteArray, 0, byteArray.Length);
                    fileStream.Close();
                }
                Logger.WriteLog("Get file from request. File name " + fileName, LogLevel.Usual);
                return true;
            }
            catch (Exception e)
            {
                Console.WriteLine("Error in function CreateFileBinary, Message: " + e.Message);
                Logger.WriteLog("Error in function CreateFileBinary, Message: " + e.Message, LogLevel.Error);
                return false;
            }
        }
        public static string GetBoundary(ref string request)
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
        public static string SearchPathToFile(string nameFile, string startSearchFolder)
        {
            string findPathFile = "";
            string pathCurrent = startSearchFolder;
            string[] files = Directory.GetFiles(pathCurrent);
            foreach (string file in files)
            {
                if (file == pathCurrent + "/" + nameFile)
                {
                    return file;
                }
            }
            string[] folders = Directory.GetDirectories(pathCurrent);
            foreach (string folder in folders)
            {
                FileAttributes attr = File.GetAttributes(folder);
                if (attr.HasFlag(FileAttributes.Directory))
                {
                    findPathFile = SearchPathToFile(nameFile, folder);
                }
            }
            return findPathFile;
        }
        public static bool DeleteFile(FileD file)
        {
            bool deleted = false;
            if (file == null)
            {
                Logger.WriteLog("Input value is null, function DeleteFile()", LogLevel.Error);
                return false;
            }
            if (File.Exists(CurrentDirectory + file.file_path + file.file_name))
            {
                File.Delete(CurrentDirectory + file.file_path + file.file_name);
                deleted = Database.file.DeleteById(file.file_id);
                if (deleted == false)
                {
                    Logger.WriteLog("Database does not contain file with id=" + file.file_id, LogLevel.Usual);
                }
                Logger.WriteLog("Delete file id=" + file.file_id, LogLevel.Usual);
                return true;
            }
            else
            {
                Logger.WriteLog("Input file->" + file.file_path + file.file_name + " not exists, function DeleteFile", LogLevel.Error);
                return false;
            }
        }
        public static void ChangeDailyPath()
        {
            DailyDirectory = Convert.ToString((int)(DateTime.UtcNow - new DateTime(1970, 1, 1)).TotalSeconds) + "/";
            Logger.WriteLog("Change daily path to->" + DailyDirectory, LogLevel.Usual);
        }
    }
}