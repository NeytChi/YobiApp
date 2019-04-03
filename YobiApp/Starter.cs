using System;
using System.Diagnostics;

namespace Common
{
    public class Starter
    {
        public static void Main(string[] args)
        {
            Debug.Listeners.Add(new TextWriterTraceListener(Console.Out));
            Config.Initialization();

            Server.port = Config.Port;
            Server.ip = Config.IP;
            Server.domen = Config.Domen;

            Database.Initialization(false);

            Worker.Initialization();

            if (args.Length != 0)
            {
                switch (args[0])
                {
                    case "-r":
                        Logger.ReadStringLogs();
                        break;
                    case "-c":
                        break;
                    case "-h":
                    case "-help":
                        helper();
                        break;
                    default:
                        Console.WriteLine("Turn first parameter for initialize server. You can turned keys: -h or -help - to see instruction of start servers modes.");
                        break;
                }
            }
            else
            {
                Server.InitListenSocket();
            }
        }
		public static void helper()
        {
            string[] commands = { "-f [time_in_minutes]", "-r", "-u", "-d", "-c",  "-h or -help" };
            string[] description =
            {
                "Start server in full working cycle. After first key, second key set time to cycle for upper program. By default, it's set 5 minutes.",
                "Start reading logs from server." ,
                "Start server in non-full working cycle. Init server without upper program.",
                "Start server in default configuration settings.",
                "Start the database cleanup mode." ,
                "Helps contains 5 modes of the server that cound be used."
            };
            Console.WriteLine();
            for (int i = 0; i < commands.Length; i++) { Console.WriteLine(commands[i] + "\t - " + description[i]); }
        }
    }
}
