using System;
using System.Threading.Tasks;
using System.Threading;
using Telegram.Bot;
using Telegram.Bot.Polling;
using Telegram.Bot.Types.Enums;
using System.Diagnostics;
using System.Net.Http;
using System.Net;
using System.IO;
using Telegram.Bot.Types;
using Message = NekoBot.Types.Message;

namespace NekoBot
{
    public enum DebugType
    {
        Debug,
        Info,
        Warning,        
        Error
    }
    public partial class Core
    {
        public static TelegramBotClient botClient;
        public static string Token = "";
        public static string BotUsername { get; private set; } = "";
        public static DateTime startTime;
        public static BotCommand[] BotCommands = Array.Empty<BotCommand>();

        static void Main(string[] args)
        {
            ScriptManager.Init();
            Monitor.Init();
            Config.Init();
            startTime = DateTime.Now;
            Config.Load(Path.Combine(Config.DatabasePath, "Proxy.config"),out string proxyStr);
            HttpClient httpClient = new(new SocketsHttpHandler
            {
                Proxy = new WebProxy(proxyStr)
                {
                    Credentials = new NetworkCredential("", "")
                },
                UseProxy = true,
            });
            botClient = new TelegramBotClient(Token, httpClient);
            Debug(DebugType.Info, "Connecting to telegram...");
            botClient.ReceiveAsync(
                updateHandler: UpdateHandleAsync,
                pollingErrorHandler: (botClient,e,cToken) => 
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
            

            while (BotUsername == "")
            {
                
                try
                {
                    BotUsername = botClient.GetMeAsync().Result.Username ?? "";
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
            while(true)
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

                    /// To-Dos
                }
                catch (Exception e)
                {
                    Debug(DebugType.Error, $"Failure to receive message : \n{e.Message}\n{e.StackTrace}");
                }
                stopwatch.Stop();
                Config.TotalHandleCount++;
                Config.TimeSpentList.Add((int)stopwatch.ElapsedMilliseconds);
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

            await Console.Out.WriteLineAsync($"[{time}][{_type}] {message}");
            LogManager.WriteLog($"[{time}][{_type}] {message}");
        }
    }
}
