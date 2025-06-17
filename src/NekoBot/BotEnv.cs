using System;
using System.IO;
using System.Threading;

namespace NekoBot;

public static class BotEnv
{
    static readonly CancellationTokenSource _cts = new();

    public static DateTime Up { get; } = DateTime.Now;
    public static string AppPath { get; } = Environment.CurrentDirectory;
    public static string LogPath { get; } = Path.Combine(AppPath, "Logs");
    public static string DataPath { get; } = Path.Combine(AppPath, "Data");
    public static string TempPath { get; } = Path.Combine(AppPath, "Temps");
    public static string ScriptPath { get; } = Path.Combine(AppPath, "Scripts");
    public static string ConfigPath { get; } = Path.Combine(AppPath, "config.yaml");
    public static CancellationToken GlobalCanncellationToken { get; } = _cts.Token;
    internal static event EventHandler OnProcessExit
    {
        add
        {
            _mainDomin.ProcessExit += value;
        }
        remove
        {
            _mainDomin.ProcessExit -= value;
        }
    }

    static readonly AppDomain _mainDomin = AppDomain.CurrentDomain;

    static BotEnv()
    {
        CreateDirectoryIfNotExists(LogPath);
        CreateDirectoryIfNotExists(DataPath);
        CreateDirectoryIfNotExists(TempPath);
        CreateDirectoryIfNotExists(ScriptPath);
        OnProcessExit += OnProcessExitFunc;
    }

    static void OnProcessExitFunc(object? sender, EventArgs e)
    {
        _cts.Cancel();
    }
    static void CreateDirectoryIfNotExists(string path)
    {
        if (!Directory.Exists(path))
        {
            Directory.CreateDirectory(path);
        }
    }
}
