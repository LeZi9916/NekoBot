using System;
using System.IO;
using NekoBot.Types;
using YamlDotNet.Serialization;

namespace NekoBot
{
    public class Config
    {
        [YamlIgnore]
        public static DateTime Up { get; set; }
        [YamlIgnore]
        public static string AppPath { get => Environment.CurrentDirectory; }
        [YamlIgnore]
        public static string LogsPath { get => Path.Combine(AppPath, "logs"); }
        [YamlIgnore]
        public static string DatabasePath { get => Path.Combine(AppPath, "Database"); }
        [YamlIgnore]
        public static string TempPath { get => Path.Combine(AppPath, "Temp"); }
        [YamlIgnore]
        public static string LogFile { get => Path.Combine(LogsPath, $"{Up.ToString("yyyy-MM-dd HH-mm-ss")}.log"); }
        [YamlIgnore]
        public static HotpAuthenticator Authenticator { get; set; } = new HotpAuthenticator();

        public long TotalHandleCount { get; set; } = 0;
        public long TotalHandleTime { get; set; } = 0;
        public static void Check()
        {
            if (!Directory.Exists(LogsPath))
                Directory.CreateDirectory(LogsPath);
            if (!Directory.Exists(DatabasePath))
                Directory.CreateDirectory(DatabasePath);
            if (!Directory.Exists(TempPath))
                Directory.CreateDirectory(TempPath);
        }
    }
}
