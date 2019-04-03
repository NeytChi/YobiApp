using System;
using Common;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml.Linq;
using Iteedee.ApkReader;
using System.IO.Compression;
using YobiApp.NDatabase.AppData;
using Common.NDatabase.FileData;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace YobiApp.Functional.Upload
{
    public class UploadApp
    {
        private Random random = new Random();
        private string Full_Path_Plist = "";
        private string Path_Relative_Plist = "/Plist/";
        public  string Full_Path_Upload = "";
        private string Path_Relative_Upload = "/Files/Upload/";
        private string KeyValueSet = "(?<=<string>)(.*)(?=</string>)";
        private string CurrentDirectory = "";
        private string Domen = "";
        private string Ssl_Domen = "";
        private string Alphavite = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789";

        public UploadApp()
        {
            Domen = Config.GetConfigValue("domen", TypeCode.String);
            Ssl_Domen = Config.GetConfigValue("ssl_domen", TypeCode.String);
            Full_Path_Plist = Config.GetConfigValue("ssl_path", TypeCode.String);
            CurrentDirectory = Directory.GetCurrentDirectory();
            Full_Path_Upload = CurrentDirectory + Path_Relative_Upload;
            Full_Path_Plist = Full_Path_Plist + Path_Relative_Plist;
            Directory.CreateDirectory(Full_Path_Upload);
            Directory.CreateDirectory(Full_Path_Plist);
        }
        public App UploadIpa(FileD file, string hash)
        {
            string pathIpa = Full_Path_Upload + hash;
            DirectoryInfo directory = Directory.CreateDirectory(pathIpa);
            try
            {
                ZipFile.ExtractToDirectory(file.file_path + file.file_name, pathIpa);
            }
            catch (Exception)
            {
                Logger.WriteLog("Cannot unzip file, name=" + file.file_name, LogLevel.Usual);
                return null;
            }
            string plistPath = SearchPathToFile("Info.plist", pathIpa);
            if (plistPath == "")
            {
                Logger.WriteLog("Cannot search plist file", LogLevel.Usual);
                return null;
            }
            App ipa = GetIpaSet(plistPath, pathIpa);
            if (ipa == null) { return null; }
            ipa.app_id = file.file_id;
            ipa.app_hash = hash;
            ipa.archive_name = file.file_name;
            ipa.url_manifest = "http://" + Domen + "/YobiApp" + Path_Relative_Upload + ipa.app_hash + "/" + ipa.archive_name;
            ipa.install_link = "itms-services://?action=download-manifest&url=https://" + Ssl_Domen + "/Plist/" + ipa.app_hash + ".plist";
            if (!CreateInstallPlist(ipa)) { return null; }
            Logger.WriteLog("Uploaded Ipa file. Hash=" + hash, LogLevel.Usual);
            return ipa;
        }
        private App GetIpaSet(string plistPath, string pathIpa)
        {
            Dictionary<string, string> xml = GetXML(plistPath);
            App ipa = new App();
            if (xml.ContainsKey("CFBundleName"))
                ipa.app_name = xml["CFBundleName"];
            if (xml.ContainsKey("CFBundleIconFiles"))
            {
                string answer = SearchPathRelativeFileName(xml["CFBundleIconFiles"], pathIpa);
                ipa.url_icon = "http://" + Domen + "/YobiApp" + answer.Substring(CurrentDirectory.Length);
            }
            if (xml.ContainsKey("CFBundleShortVersionString"))
                ipa.version = xml["CFBundleShortVersionString"];
            if (xml.ContainsKey("CFBundleVersion"))
                ipa.build = xml["CFBundleVersion"];
            if (xml.ContainsKey("CFBundleIdentifier"))
                ipa.bundleIdentifier = xml["CFBundleIdentifier"];
            Logger.WriteLog("Get IPA set info", LogLevel.Usual);
            return ipa;
        }
        private bool CreateInstallPlist(App ipa)
        {
            XElement xAssets = new XElement("key", "assets");
            XElement xFirstDict = new XElement("dict", new XElement("key", "kind"),
                                                       new XElement("string", "software-package"),
                                                       new XElement("key", "url"),
                                                       new XElement("string", ipa.url_manifest));
            XElement xFirstArray = new XElement("array", xFirstDict);
            XElement xMetadata = new XElement("key", "metadata");
            XElement xSecondDict = new XElement("dict", new XElement("key", "bundle-identifier"),
                                                        new XElement("string", ipa.bundleIdentifier),
                                                        new XElement("key", "bundle-version"),
                                                        new XElement("string", "4.0"),
                                                        new XElement("key", "kind"),
                                                        new XElement("string", "software"),
                                                        new XElement("key", "title"),
                                                        new XElement("string", ipa.app_name));
            XElement xThirdDict = new XElement("dict", xAssets, xFirstArray, xMetadata, xSecondDict);
            XElement xItems = new XElement("key", "items");
            XElement xSecondArray = new XElement("array", xThirdDict);
            XElement xFourDict = new XElement("dict", xItems, xSecondArray);
            XElement xPlist = new XElement("plist", new XAttribute("version", "1.0"), xFourDict);
            XDocument xDocument = new XDocument(xPlist);
            xDocument.AddFirst(new XDocumentType("plist", "-//Apple//DTD PLIST 1.0//EN", "http://www.apple.com/DTDs/PropertyList-1.0.dtd", null));
            Directory.CreateDirectory(Full_Path_Plist);
            xDocument.Save(Full_Path_Plist + ipa.app_hash + ".plist");
            Logger.WriteLog("Create install plist", LogLevel.Usual);
            return true;
        }
        private string SetManifestLinkToPlist(string plistInfo, string pathIpaArch)
        {
            int searchStart = plistInfo.IndexOf("CFBundleIdentifier", StringComparison.Ordinal) - "<key>".Length;
            if (searchStart != -1)
            {
                pathIpaArch = "https://" + Domen + "/YobiApp" + pathIpaArch.Substring(CurrentDirectory.Length);
                string insertValue = "<key>url</key>" + "<string>" + pathIpaArch + "</string>";
                string value = plistInfo.Insert(searchStart, insertValue);
                return value;
            }
            return "";
        }
        private string SetKeyValueXML(string xml, string key, string value)
        {
            int start = xml.IndexOf(@"<key>" + key + @"</key>", StringComparison.Ordinal);
            if (start == -1) { return ""; }
            Regex regex = new Regex(KeyValueSet, RegexOptions.Multiline);
            Match match = regex.Match(xml, start);
            if (match.Success)
            {
                xml = xml.Remove(match.Index, match.Length);
                xml = xml.Insert(match.Index, value);
                return xml;
            }
            else
            {
                return xml;
            }
        }
        private Dictionary<string, string> GetXML(string pathPList)
        {
            XDocument docs = XDocument.Load(pathPList);
            var elements = docs.Descendants("dict");
            Dictionary<string, string> keyValues = new Dictionary<string, string>();
            keyValues = docs.Descendants("dict")
            .SelectMany(d => d.Elements("key")
            .Zip(d.Elements()
            .Where(e => e.Name != "key"), (k, v) => new { Key = k, Value = v }))
            .ToDictionary(i => i.Key.Value, i => i.Value.Value);
            return keyValues;
        }
        private string SearchPathToFile(string nameFile, string startSearchFolder)
        {
            string findPathFile = "";
            string pathCurrent = startSearchFolder;
            string[] files = Directory.GetFiles(pathCurrent);
            foreach (string file in files)
            {
                if (file == pathCurrent + "/" + nameFile) { return file; }
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
        private string SearchPathRelativeFileName(string nameFile, string startSearchFolder)
        {
            string[] paths = Directory.GetFiles(startSearchFolder, nameFile + "*", SearchOption.AllDirectories);
            return paths[0];
        }
        public App UploadAPK(FileD file, string hash)
        {
            string pathAPK = Full_Path_Upload + hash;
            DirectoryInfo directory = Directory.CreateDirectory(pathAPK);
            try
            {
                ZipFile.ExtractToDirectory(file.file_path + file.file_name, pathAPK);
            }
            catch (Exception)
            {
                Logger.WriteLog("Cannot unzip file, name=" + file.file_name, LogLevel.Error);
                return null;
            }
            App apk = new App
            {
                app_id = file.file_id,               
                app_hash = hash,
                archive_name = file.file_last_name,
                url_manifest = "http://" + Domen + "/YobiApp" + Path_Relative_Upload + hash + "/" + file.file_name,
                install_link = "http://" + Domen + "/YobiApp" + Path_Relative_Upload + hash + "/" + file.file_name    
            };
            Logger.WriteLog("Uploaded APK file. Hash=" + hash, LogLevel.Usual);
            return apk;
        }
        public App GetAPKSet(string pathAPK, App apk)
        {
            ApkInfo info = GetAndroidInfo(pathAPK);
            if (info == null) return apk;
            apk.app_name = info.packageName;
            apk.url_icon = "http://" + Domen + "/YobiApp" + (pathAPK + info.iconFileName[0]).Substring(CurrentDirectory.Length);
            apk.version = info.versionName;
            apk.build = info.targetSdkVersion;
            apk.bundleIdentifier = info.label;
            return apk;
        }
        private ApkInfo GetAndroidInfo(string path)
        {
            byte[] manifestData = null;
            byte[] resourcesData = null;
            string manifestPath = SearchPathToFile("AndroidManifest.xml", path);
            if (manifestPath == "")
            {
                Logger.WriteLog("Cannot search AndroidManifest.xml file", LogLevel.Usual);
                return null;
            }
            string resourcesPath = SearchPathToFile("resources.arsc", path);
            if (resourcesPath == "")
            {
                Logger.WriteLog("Cannot search resources.arsc file", LogLevel.Usual);
                return null;
            }
            manifestData = File.ReadAllBytes(manifestPath);
            resourcesData = File.ReadAllBytes(resourcesPath);
            if (manifestData == null || resourcesData == null) { return null; }
            ApkReader apkReader = new ApkReader();
            ApkInfo info = apkReader.extractInfo(manifestData, resourcesData);
            Logger.WriteLog("Get android info from archive", LogLevel.Usual);
            return info;
        }
        public bool deleteAllOldFiles()
        {
            DateTime deleteTime = new DateTime(DateTime.Now.Year,
                                               DateTime.Now.Month,
                                               DateTime.Now.Day - 5,
                                               DateTime.Now.Hour,
                                               DateTime.Now.Minute,
                                               DateTime.Now.Second);
            List<App> apps = Database.app.SelectLessMassTime(deleteTime);
            foreach (App app in apps)
            {
                Database.file.DeleteById(app.app_id);
                DeleteDirectory(app.app_hash);
            }
            Logger.WriteLog("Delete all old app files", LogLevel.Usual);
            return true;
        }
        public bool DeleteDirectory(string hash)
        {
            if (Directory.Exists(Full_Path_Upload + hash))
            {
                Directory.Delete(Full_Path_Upload + hash, true);
            }
            if (File.Exists(Full_Path_Plist + hash + ".plist"))
            {
                File.Delete(Full_Path_Plist + hash + ".plist");
            }
            Database.app.Delete(hash);
            Logger.WriteLog("Delete app directory, hash=" + hash, LogLevel.Usual);
            return true;
        }
        private string GenerateId()
        {
            int firstArg = random.Next(100000000, 999999999);
            int secondArg = random.Next(100000, 999999);
            string id = firstArg.ToString() + secondArg.ToString();
            return id;
        }
        public string GenerateHash(int length)
        {
            string hash = "";
            for (int i = 0; i < length; i++)
            {
                hash += Alphavite[random.Next(Alphavite.Length)];
            }
            return hash;
        }
        private string readFile(string path)
        {
            string textFromFile = "";
            using (FileStream fstream = File.OpenRead(path))
            {
                byte[] array = new byte[fstream.Length];
                fstream.Read(array, 0, array.Length);
                textFromFile = Encoding.Default.GetString(array);
                fstream.Close();
            }
            return textFromFile;
        }
        private bool writeFile(string path, string text)
        {
            using (FileStream fstream = new FileStream(path, FileMode.Create, FileAccess.ReadWrite))
            {
                byte[] array = Encoding.ASCII.GetBytes(text);
                fstream.Write(array, 0, array.Length);
                fstream.Close();
                return true;
            }
        }
    }
}