using System;
using System.Threading.Tasks;
using System.Threading;
using Telegram.Bot;
using Telegram.Bot.Polling;
using Telegram.Bot.Types.Enums;
using System.Net.Http;
using System.Net;
using Telegram.Bot.Types;


namespace NekoBot;
public class Core
{
    static void Main(string[] args)
    {
        var botConfig = BotEnv.Config;
        if (string.IsNullOrEmpty(botConfig.Token))
        {
            BotLogger.Fatal("Bot token not found");
        }
        var botClient = new TelegramBotClient(botConfig.Token, BotEnv.SharedHttpClient);
        BotLogger.Info("Connecting to telegram...");
        botClient.ReceiveAsync(updateHandler: OnReceivedUpdateAsync,
                               errorHandler: OnPollingErrorAsync,
                               cancellationToken: BotEnv.GlobalCanncellationToken);
        while (true)
        {
            Console.ReadKey();
        }
    }
    static async Task OnReceivedUpdateAsync(ITelegramBotClient client,
                                            Update update,
                                            CancellationToken token)
    {

    }
    static async Task OnPollingErrorAsync(ITelegramBotClient client,
                                          Exception e,
                                          CancellationToken token)
    {

    }
}
