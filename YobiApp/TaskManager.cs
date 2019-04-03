using Common;
using FluentScheduler;
using YobiApp.Functional.Upload;

namespace YobiApp.Functional.Tasker
{
    public static class TaskManager
    {
        private static void SetNewLogFile()
        {
            Registry registry = new Registry();
            registry.Schedule(() => Logger.SetUpNewLogFile()).ToRunEvery(1).Days().At(6, 0);
            Logger.WriteLog("Task manager start Daily_Regimen method", LogLevel.Usual);
            JobManager.InitializeWithoutStarting(registry);
            JobManager.Start();
        }
        private static void DeleteOldFiles()
        {
            Registry registry = new Registry();
            registry.Schedule(() => new UploadApp().deleteAllOldFiles()).ToRunEvery(1).Days().At(6, 0);
            Logger.WriteLog("Task manager start Daily_Regimen method", LogLevel.Usual);
            JobManager.InitializeWithoutStarting(registry);
            JobManager.Start();
        }
    }
}
