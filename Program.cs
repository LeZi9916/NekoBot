using System;
using System.Threading.Tasks;
using System.Threading;
using Telegram.Bot;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Exceptions;
using System.Collections.Generic;
using System.Diagnostics;

namespace TelegramBot
{
    enum DebugType
    {
        Info,
        Warning,
        Debug,
        Error
    }
    internal partial class Program
    {
        static TelegramBotClient botClient;
        static string Token = "6872337338:AAEylSFo-QmT0B49_q_oF3Cy6ah6PbxkCxI";
        static string BotUsername = "ZzyFuckComputerbot";
        static DateTime startTime;
        static Stopwatch stopwatch = new Stopwatch();
        static void Main(string[] args)
        {
            Monitor.Init();
            Config.Init();
            startTime = DateTime.Now;
            botClient = new TelegramBotClient(Token);

            botClient.StartReceiving(
                updateHandler: UpdateHandleAsync,
                pollingErrorHandler: HandlePollingErrorAsync,
                receiverOptions: new ReceiverOptions()
                {
                    AllowedUpdates = Array.Empty<UpdateType>()
                }
            );
            Debug(DebugType.Info,"Connect Successful\n");
            while(true)
                Console.ReadKey();
        }
        static async Task MessageHandleAsync(ITelegramBotClient botClient, Update update)
        {
            await Task.Run(() =>
            {
                try
                {
                    var message = update.Message;
                    var chat = message.From;
                    var userId = message.From.Id;

                    var text = message.Text is null ? message.Caption ?? "" : message.Text;
                    CommandPreHandle(text.Split(" "), update);

                    Debug(DebugType.Info, $"Received message:\n" +
                        $"Chat Type: {message.Chat.Type}\n" +
                        $"Message Type: {message.Type}\n" +
                        $"Form User: {chat.FirstName} {chat.LastName}[@{chat.Username}]({userId})\n" +
                        $"Content: {message.Text ?? ""}\n");
                }
                catch(Exception e)
                {
                    Debug(DebugType.Error, $"Failure to receive message : {e.Message}");
                }
            });
        }
        static async Task UpdateHandleAsync(ITelegramBotClient botClient, Update update, CancellationToken cancellationToken)
        {
            stopwatch.Reset();
            stopwatch.Start();
            try
            {
                CheckUser(update);

                switch (update.Type)
                {
                    case UpdateType.Message:
                        await MessageHandleAsync(botClient, update);
                        break;
                    case UpdateType.InlineQuery:
                        break;
                    case UpdateType.ChosenInlineResult:
                        break;
                    case UpdateType.CallbackQuery:
                        break;
                    case UpdateType.EditedMessage:
                        break;
                    case UpdateType.ChannelPost:
                        break;
                    case UpdateType.EditedChannelPost:
                        break;
                    case UpdateType.ShippingQuery:
                        break;
                    case UpdateType.PreCheckoutQuery:
                        break;
                    case UpdateType.Poll:
                        break;
                    case UpdateType.PollAnswer:
                        break;
                    case UpdateType.MyChatMember:
                        break;
                    case UpdateType.ChatMember:
                        break;
                    case UpdateType.ChatJoinRequest:
                        break;
                    case UpdateType.Unknown:
                        break;
                    default:
                        break;
                }
            }
            catch (Exception e)
            {
                Debug(DebugType.Error, $"Failure to handle message : {e.Message}");
                return;
            }            
            stopwatch.Stop();
            Config.TotalHandleCount++;
            Config.TimeSpentList.Add((int)stopwatch.ElapsedMilliseconds);
        }
        static Task HandlePollingErrorAsync(ITelegramBotClient botClient, Exception exception, CancellationToken cancellationToken)
        {
            Debug(DebugType.Error, $"From internal exception : {exception.Message}");
            return Task.CompletedTask;
        }
        static async void CheckUser(Update update)
        {
            await Task.Run(() => 
            {
                var message = update.Message ?? update.EditedMessage ?? update.ChannelPost;
                if (message is null)
                    return;

                User[] userList = new User[4];
                userList[0] = message.From;
                userList[1] = message.ForwardFrom;
                if (message.ReplyToMessage is not null)
                {
                    userList[2] = message.ReplyToMessage.From;
                    userList[3] = message.ReplyToMessage.ForwardFrom;
                }
                

                foreach (var user in userList)
                {
                    if (user is null)
                        continue;
                    if (Config.UserIdList.Contains(user.Id))
                        continue;

                    Config.AddUser(new TUser()
                    {
                        Id = user.Id,
                        Username = user.Username,
                        FirstName = user.FirstName,
                        LastName = user.LastName
                    });

                    Debug(DebugType.Info, $"Find New User:\n" +
                    $"Name: {user.FirstName} {user.LastName}\n" +
                    $"isBot: {user.IsBot}\n" +
                    $"Username: {user.Username}\n" +
                    $"isPremium: {user.IsPremium}\n");
                }
            });
        }
        public static async void Debug(DebugType type, string message)
        {
            await Task.Run(() =>
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

                Console.WriteLine($"[{time}][{_type}] {message}");
                Config.WriteLog($"[{time}][{_type}] {message}");
            });
        }
    }
}
