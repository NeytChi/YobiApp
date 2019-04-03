using System;
using Common;
using System.IO;
using System.Net.Sockets;
using Common.NDatabase.FileData;
using YobiApp.NDatabase.AppData;
using System.Collections.Generic;
using YobiApp.Functional.FileWork;

namespace YobiApp.Functional.Upload
{
    public class UploadF
    {
        int count = 1;

        public void UploadBuild(ref string request, ref byte[] buffer, ref int bytes, ref Socket remoteSocket)
        {
            List<FileD> files = LoaderFile.LoadingFiles(ref request, ref buffer, ref bytes, ref count);
            if (files != null)
            {
                if (files.Count == 1)
                {
                    FileD file = files[0];
                    string app_hash = Worker.uploader.GenerateHash(8);
                    file.file_path = Worker.uploader.Full_Path_Upload + app_hash + "/";
                    file.file_name = file.file_last_name;
                    if (file.file_type == "application")
                    {
                        Directory.CreateDirectory(file.file_path);
                        LoaderFile.CreateFileBinary(file.file_path + file.file_name, ref file.buffer);
                        App app = null;
                        switch(file.file_extension)
                        {
                            case "vnd.android.package-archive":
                                app = Worker.uploader.UploadAPK(file, app_hash);
                                break;
                            case "octet-stream":
                                app = Worker.uploader.UploadIpa(file, app_hash);
                                break;
                            default:
                                LoaderFile.DeleteFile(file);
                                Worker.uploader.DeleteDirectory(app_hash);
                                Worker.JsonAnswer(false, "Archive type that uploaded is wrong.", ref remoteSocket);
                                break;
                        }
                        if (app == null)
                        {
                            LoaderFile.DeleteFile(file);
                            Worker.uploader.DeleteDirectory(app_hash);
                            Worker.JsonAnswer(false, "Error of handle upload archive.", ref remoteSocket);
                            return;
                        }
                        int? uid = Worker.FindParamFromRequest(ref request, "uid", TypeCode.Int32);
                        if (uid != null)
                        {
                            if (Database.user.SelectId(uid) != null)
                            {
                                app.user_id = (int)uid;
                            }
                        }
                        else
                        {
                            app.user_id = -1;
                        }
                        Worker.JsonData(app, ref remoteSocket);
                        if (file.file_extension == "vnd.android.package-archive")
                        {
                            app = Worker.uploader.GetAPKSet(Worker.uploader.Full_Path_Upload + app_hash, app);
                        }
                        Database.app.Add(app);
                    }
                    else
                    {
                        Worker.JsonAnswer(false, "Wrong type of file.", ref remoteSocket);
                    }
                }
                else
                {
                    foreach(FileD file in files)
                    {
                        LoaderFile.DeleteFile(file);
                    }
                    Worker.JsonAnswer(false, "Get from request not required count files.", ref remoteSocket);
                }
            }
            else
            {
                Worker.JsonAnswer(false, "Can't get file from request.", ref remoteSocket);
            } 
        }    
        public void SelectMassApps(ref string request, ref Socket remoteSocket)
        {
            int? user_id = Worker.FindParamFromRequest(ref request, "user_id", TypeCode.Int32);
            if (user_id == null)
            {
                Worker.JsonAnswer(false, "Can not find id in this request params.", ref remoteSocket);
                return;
            }
            if (Database.user.SelectId(user_id) == null)
            {
                Worker.JsonAnswer(false, "Can not find user with this user_id.", ref remoteSocket);
                return;
            }
            List<App> apps = Database.app.SelectByUserId(user_id);
            if (apps.Count == 0)
            {
                Worker.JsonAnswer(false, "This user doesn't has any builds.", ref remoteSocket);
                return;
            }
            Worker.JsonData(apps, ref remoteSocket);
        }
        public void DeleteApp(ref string request, ref Socket remoteSocket)
        {
            string hash = Worker.FindParamFromRequest(ref request, "hash", TypeCode.String);
            if (hash == null)
            {
                Worker.JsonAnswer(false, "Can not find hash in this request params.", ref remoteSocket);
                return;
            }
            App app = Database.app.SelectByHash(hash);
            if (app == null)
            {
                Worker.JsonAnswer(false, "Can not find application by this hash.", ref remoteSocket);
                return;
            }
            FileD file = Database.file.SelectById(app.app_id);
            if (file != null)
            {
                LoaderFile.DeleteFile(file);
            }
            Worker.uploader.DeleteDirectory(app.app_hash);
            Worker.JsonAnswer(true, "Build completely deleted.", ref remoteSocket);
        }
        public void SelectApp(ref string request, ref Socket remoteSocket)
        {
            string app_hash = Worker.FindParamFromRequest(ref request, "hash", TypeCode.String);
            if (app_hash == null)
            {
                Worker.JsonAnswer(false, "Can not find hash in request =" + app_hash, ref remoteSocket);
                return;
            }
            App app = Database.app.SelectByHash(app_hash);
            if (app == null)
            {
                Worker.JsonAnswer(false, "Can not find application by hash =" + app_hash, ref remoteSocket);
                return;
            }
            Worker.JsonData(app, ref remoteSocket);
        }
    }
}
