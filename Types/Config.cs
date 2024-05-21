using System;
using System.IO;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace NekoBot.Types
{
    public class Proxy
    {
        public bool UseProxy { get; set; } = false;
        public string? Address { get; set; } = null;
    }
    public class Analyzer
    {
        public long TotalHandleCount { get; set; } = 0;
        public long TotalHandleTime { get; set; } = 0;
    }
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
        public static string ConfigPath { get => Path.Combine(AppPath, "NekoBot.conf"); }

        public HotpAuthenticator Authenticator { get; set; } = new HotpAuthenticator();
        public bool DbAutoSave { get; set; } = true;
        public int AutoSaveInterval { get; set; } = 600;
        public string Token { get; set; } = "";
        public Proxy Proxy { get; set; } = new();
        public Analyzer Analyzer { get; set; } = new();

        public static void Check()
        {
            if (!Directory.Exists(LogsPath))
                Directory.CreateDirectory(LogsPath);
            if (!Directory.Exists(DatabasePath))
                Directory.CreateDirectory(DatabasePath);
            if (!Directory.Exists(TempPath))
                Directory.CreateDirectory(TempPath);
        }
        public string Serialize<T>(T obj)
        {
            var serializer = new SerializerBuilder().Build();
            return serializer.Serialize(obj);
        }
        public T? Deserialize<T>(string yaml)
        {
            var deserializer = new DeserializerBuilder().Build();
            return deserializer.Deserialize<T>(yaml);
        }
    }
}
