using CSScripting;
using NekoBot;
using NekoBot.Types;
using System;
using System.Data;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Message = NekoBot.Types.Message;

namespace NekoBot
{
    public partial class Core
    {
        static void CommandPreHandle(Message userMsg)
        {
            if (userMsg.Command is null)
                return;

            var group = userMsg.IsGroup ? Config.SearchGroup(userMsg.Chat.Id) : null;
            var user = userMsg.From;
            var cmd = (Command)userMsg.Command;

            // Reference check
            if (group is not null && group.Setting.ForceCheckReference)
            {
                if (!cmd.Prefix.Contains("@")) 
                    return;

                var prefix = cmd.Prefix;
                var s = prefix.Split("@", 2, StringSplitOptions.RemoveEmptyEntries);

                if (s.Length != 2 || s[1] != BotUsername)
                    return;                
            }
            if(cmd.Prefix.Contains("@"))
            {
                cmd.Prefix = cmd.Prefix.Split("@",StringSplitOptions.RemoveEmptyEntries).First();
                userMsg.Command = cmd;
            }

            // Sender check
            if (!user.IsNormal)
            {
                var sArray = string.Join(" ", cmd.Params).Split("-token", StringSplitOptions.RemoveEmptyEntries);
                if (sArray.Length != 2)
                {
                    //SendMessage("Authentication failed:\nGroup or channel anonymous access is not allowed", update);
                    Debug(DebugType.Info, "Channel access,rejected");
                    return;
                }
                var token = sArray[1];

                if (!Config.Authenticator.Compare(token.Trim()))
                {
                    //SendMessage("Authentication failed:\nInvalid HOTP code", update);
                    Debug(DebugType.Info, "HOTP code is invalid,rejected");
                    return;
                }
                else
                    cmd.Params = sArray[0].Split(" ", StringSplitOptions.RemoveEmptyEntries);
            }
            // Banned user check
            if (!user.CheckPermission(Permission.Root) &&
                !user.CheckPermission(Permission.Common, group))
            {
                //SendMessage("Access Denied", update);
                Debug(DebugType.Info, "Banned user access,rejected");
                return;
            }
            Debug(DebugType.Debug,"User Request:\n" +
                $"From: {userMsg.From.Name}({userMsg.From.Id})\n" +
                $"Chat: {userMsg.Chat.Id}\n" +
                $"Permission: {userMsg.From.Level.ToString()}\n" +
                $"Prefix: /{cmd.Prefix}\n" +
                $"Params: {string.Join(" ", cmd.Params)}");
            ScriptManager.CommandHandle(userMsg);
        }
    }
}
