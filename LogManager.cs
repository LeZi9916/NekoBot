using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using TelegramBot;

namespace NekoBot
{
    public static class LogManager
    {
        static Mutex mutex = new();
        static string LogFile { get => Config.LogFile; }
        static string LogsPath { get => Config.LogsPath; }
        public static int LogCount { get; private set; } = 0;
        public static void WriteLog(string s)
        {
            mutex.WaitOne();
            File.AppendAllTextAsync(LogFile, $"{s}\n", Encoding.UTF8);
            LogCount++;
            mutex.ReleaseMutex();
        }
        static DebugType GetLogLevel(string s)
        {
            if (s.Contains("[Info]"))
                return DebugType.Info;
            else if (s.Contains("[Debug]"))
                return DebugType.Debug;
            else if (s.Contains("[Warning]"))
                return DebugType.Warning;
            else
                return DebugType.Error;
        }
        public static string[] GetLog(int count, DebugType logLevel = DebugType.Error)
        {
            string[] logs = File.ReadAllLines(LogFile);
            List<string> result = new();
            string s = "";

            for (int index = logs.Length - 1; index >= 0; index--)
            {
                if (string.IsNullOrEmpty(logs[index]))
                    continue;
                else if (logs[index][0] == '[')
                {
                    s = $"{logs[index]}\n{s}";
                    if (GetLogLevel(s) >= logLevel)
                        result.Add(s);
                    s = "";
                }
                else
                    s = $"{logs[index]}\n{s}";
                if (index <= 0)
                    break;
                else if (result.Count >= count)
                    break;
            }
            return result.ToArray();
        }
    }
}
