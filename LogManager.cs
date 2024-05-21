using NekoBot.Types;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using YamlDotNet.Serialization;

namespace NekoBot
{
    public static class LogManager
    {
        static Mutex mutex = new();
        static string LogFile { get => Config.LogFile; }
        static string LogsPath { get => Config.LogsPath; }
        public static int LogCount { get; private set; } = 0;

        static List<Log> logs = new();
        public static void WriteLog(Log l)
        {
            mutex.WaitOne();
            logs.Add(l);
            File.WriteAllText(LogFile, Serializer.Yaml.Serialize(logs), Encoding.UTF8);
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
        public static Log[] GetLog(int count, DebugType logLevel = DebugType.Error)
        {
            var logs = File.ReadAllText(LogFile);
            var result = Serializer.Yaml.Deserialize<Log[]>(logs) ?? Array.Empty<Log>();

            var filtered = result.Where(x => x.Level >= logLevel);

            return filtered.TakeLast(count).ToArray();
        }
    }
}
