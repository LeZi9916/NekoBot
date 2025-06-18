using NekoBot.Clients;
using System;
using System.Threading.Tasks;

namespace NekoBot;
public class Program
{
    static async Task Main(string[] args)
    {
        var botConfig = BotEnv.Config;
        if (string.IsNullOrEmpty(botConfig.Token))
        {
            BotLogger.Fatal("Bot token not found");
        }

        await TGBotClient.Start();
    }
}
