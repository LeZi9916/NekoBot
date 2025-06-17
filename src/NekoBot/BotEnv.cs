using System;
using System.IO;
using System.Threading;

namespace NekoBot;

public static class BotEnv
{
    static readonly CancellationTokenSource _cts = new();

    public static DateTime Up { get; } = DateTime.Now;
    public static string AppPath { get; } = Environment.CurrentDirectory;
    public static string LogsPath { get; } = Path.Combine(AppPath, "logs");
    public static string DatabasePath { get; } = Path.Combine(AppPath, "Database");
    public static string TempPath { get; } = Path.Combine(AppPath, "Temp");
    public static string ScriptPath { get; } = Path.Combine(AppPath, "Scripts");
    public static string LogFile { get; } = Path.Combine(LogsPath, $"{Up.ToString("yyyy-MM-dd HH-mm-ss")}.log");
    public static string ConfigPath { get; } = Path.Combine(AppPath, "NekoBot.conf");
    public static CancellationToken GlobalCanncellationToken { get; } = _cts.Token;

    static BotEnv()
    {

    }
}
