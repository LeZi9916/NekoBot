using NekoBot.Types;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Telegram.Bot;
using Telegram.Bot.Types;

namespace NekoBot.Clients;
internal static class TGBotClient
{
    public static BotInfo Info { get; private set; }

    static long _botId = 0;
    static TelegramBotClient? _botClient = null;
    public static async Task StartAsync()
    {
        _botClient = new TelegramBotClient(BotEnv.Config.Token, BotEnv.SharedHttpClient);
        BotLogger.Info("Connecting to telegram server...");
        _botClient.ReceiveAsync(updateHandler: OnReceivedUpdateAsync,
                                errorHandler: OnPollingErrorAsync,
                                cancellationToken: BotEnv.GlobalCanncellationToken);
        var isValidToken = await _botClient.TestApi();
        BotLogger.Info("Connected");
        if(!isValidToken)
        {
            BotLogger.Fatal("Invalid bot token");
        }
        BotLogger.Debug("Token is valid");
        _botId = _botClient.BotId;
        Info = new()
        {
            Id = _botId,
            FirstName = string.Empty,
            LastName = string.Empty,
        };
        BotLogger.Debug($"Bot id: {_botClient.BotId}");
        do
        {
            try
            {
                var rsp = await _botClient.GetMe();
                Info = new()
                {
                    Id = _botId,
                    FirstName = rsp.FirstName,
                    LastName = rsp.LastName ?? string.Empty,
                };
            }
            catch(OperationCanceledException)
            {
                break;
            }
            catch (Exception e)
            {
                BotLogger.Error("Failed to get bot information", e);
            }
            finally
            {
                await Task.Delay(1000 * 60 * 10);
            }
        } while (true);
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
