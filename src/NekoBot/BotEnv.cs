using NekoBot.Text;
using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Threading;

namespace NekoBot;

public static class BotEnv
{
    static readonly CancellationTokenSource _cts = new();

    public static DateTime Up { get; } = DateTime.Now;
    public static string AppPath { get; } = Environment.CurrentDirectory;
    public static string LogPath { get; } = Path.Combine(AppPath, "logs");
    public static string DataPath { get; } = Path.Combine(AppPath, "data");
    public static string TempPath { get; } = Path.Combine(AppPath, "temps");
    public static string ScriptPath { get; } = Path.Combine(AppPath, "scripts");
    public static string ConfigPath { get; } = Path.Combine(AppPath, "config.yaml");
    public static BotConfig Config { get; } = new();
    public static HttpClient SharedHttpClient { get; }
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
        if (File.Exists(ConfigPath))
        {
            Config = Serializer.Yaml.Deserialize<BotConfig>(File.ReadAllText(ConfigPath))!;
        }
        else
        {
            BotLogger.Debug("The configuration file has been generated");
            File.WriteAllText(ConfigPath, Serializer.Yaml.Serialize(Config));
            Console.ReadKey();
            Environment.Exit(0);
        }
        BotLogger.Info($"Read bot config from {ConfigPath}");
        var proxyConfig = Config.Networking.Proxy;
        if (proxyConfig.UseProxy)
        {
            if (string.IsNullOrEmpty(proxyConfig.Address))
            {
                HttpClient.DefaultProxy = WebRequest.GetSystemWebProxy();
            }
            else
            {
                if (proxyConfig.Address.StartsWith("http://") || proxyConfig.Address.StartsWith("https://"))
                {
                    BotLogger.Fatal("Not supported proxy type");
                }
                HttpClient.DefaultProxy = new WebProxy(proxyConfig.Address)
                {
                    Credentials = new NetworkCredential(proxyConfig.Username, proxyConfig.Password)
                };
            }
            SharedHttpClient = new();
        }
        else
        {
            SharedHttpClient = new(new SocketsHttpHandler()
            {
                UseProxy = false
            });
        }
        SharedHttpClient.Timeout = TimeSpan.FromMilliseconds(Config.Networking.TimeoutMS);
        SharedHttpClient.DefaultRequestHeaders.UserAgent.Add(new("NekoBot", "0.1.0"));
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
