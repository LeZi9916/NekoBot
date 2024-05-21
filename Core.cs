using System;
using System.Threading.Tasks;
using System.Threading;
using Telegram.Bot;
using Telegram.Bot.Polling;
using Telegram.Bot.Types.Enums;
using System.Diagnostics;
using System.Net.Http;
using System.Net;
using Telegram.Bot.Types;
using Message = NekoBot.Types.Message;
using File = System.IO.File;
using NekoBot.Types;


namespace NekoBot;
public partial class Core
{
    static TelegramBotClient? botClient;

    public static string BotUsername { get; private set; } = "";
    public static BotCommand[] BotCommands { get; set; } = Array.Empty<BotCommand>();
    public static Config Config { get; set; } = new Config();

    static void Main(string[] args)
    {
        Config.Up = DateTime.Now;
        Config.Check();
        ScriptManager.Init();


        if (File.Exists(Config.ConfigPath))
            Config = Serializer.Yaml.Deserialize<Config>(File.ReadAllText(Config.ConfigPath))!;
        else
        {
            Debug(DebugType.Info, "The configuration file has been generated\n" +
                                 $"Path: {Config.ConfigPath}");
            File.WriteAllText(Config.ConfigPath, Serializer.Yaml.Serialize(Config));
            Console.ReadKey();
            Environment.Exit(0);
        }
        if (string.IsNullOrEmpty(Config.Token))
        {
            Debug(DebugType.Error, "Bot token not found");
            Console.ReadKey();
            Environment.Exit(0);
        }
        Config.AutoSave();
        if (Config.Proxy.UseProxy)
        {
            HttpClient httpClient = new(new SocketsHttpHandler
            {
                Proxy = new WebProxy(Config.Proxy.Address)
                {
                    Credentials = new NetworkCredential("", "")
                },
                UseProxy = true,
            });
            botClient = new TelegramBotClient(Config.Token, httpClient);
        }
        else
            botClient = new TelegramBotClient(Config.Token);

        Debug(DebugType.Info, "Connecting to telegram...");
        botClient.ReceiveAsync(
            updateHandler: UpdateHandleAsync,
            pollingErrorHandler: (botClient, e, cToken) =>
            {
                Debug(DebugType.Error, $"From internal exception : \n{e}");
                return Task.CompletedTask;
            },
            receiverOptions: new ReceiverOptions()
            {
                AllowedUpdates = Array.Empty<UpdateType>(),
                Limit = 30
            }
        );

        while (string.IsNullOrEmpty(BotUsername))
        {
            try
            {
                BotUsername = botClient.GetMeAsync().Result.Username ?? string.Empty;
                if (string.IsNullOrEmpty(BotUsername))
                    Debug(DebugType.Info, "Connect failure,retrying...");
                else
                {
                    Debug(DebugType.Info, "Connect Successful");
                    break;
                }
            }
            catch
            {
                Debug(DebugType.Info, "Connect failure,retrying...");
            }
        }
        ScriptManager.UpdateCommand();
        while (true)
            Console.ReadKey();
    }
    static async Task UpdateHandleAsync(ITelegramBotClient botClient, Update update, CancellationToken cancellationToken)
    {
        await Task.Run(() =>
        {
            Stopwatch stopwatch = new();
            stopwatch.Reset();
            stopwatch.Start();
            try
            {
                var msg = Message.Parse(botClient, update.Message);

                Debug(DebugType.Debug, $"Received message:\n" +
                    $"Sender : {msg.From.Name}[@{msg.From.Username}]({msg.From.Id})\n" +
                    $"From   : {msg.Chat.FirstName} {msg.Chat.LastName}({msg.Chat.Id}) |{(msg.IsGroup ? "Group" : "Private")}\n" +
                    $"Type   : {msg.Type}\n" +
                    $"Content: {(string.IsNullOrEmpty(msg.Content) ? string.Empty : msg.Content)}\n");

                ScriptManager.MessageHandle(botClient,update);
            }
            catch (Exception e)
            {
                Debug(DebugType.Error, $"Failure to receive message : \n{e.Message}\n{e.StackTrace}");
            }
            stopwatch.Stop();
            //Config.TotalHandleCount++;
            //Config.TimeSpentList.Add((int)stopwatch.ElapsedMilliseconds);
        });
    }
    public static async void Debug(DebugType type, string message)
    {
        var time = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
        var _type = type switch
        {
            DebugType.Info => "Info",
            DebugType.Warning => "Warning",
            DebugType.Debug => "Debug",
            DebugType.Error => "Error",
            _ => "Unknow"
        };

        if (_type == "Unknow")
            return;
        var log = new Log()
        { 
            Timestamp = DateTime.Now,
            Level = type, 
            Message = message 
        };
        await Console.Out.WriteLineAsync(log.ToString());
        LogManager.WriteLog(log);
    }
    public static ITelegramBotClient GetClient() => botClient!;
}
