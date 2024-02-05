using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace TelegramBot
{
    internal partial class Program
    {
        static Func<string, string> StringHandle = s =>
        {
            string[] reservedChar = { "_", "*", "[", "]", "(", ")", "~", "`", ">", "#", "+", "-", "=", "|", "{", "}", ".", "!" };
            foreach (var c in reservedChar)
                s = s.Replace(c, $"\\{c}");
            return s;

        };
        static string[] AdminCommands = { "update" };
        enum CommandType
        {
            Start,
            Add,
            Ban,
            Bind,
            Status,
            Help,
            Info,
            Promote,
            Demote,
            Mai,
            Logs,
            Config,
            Set,
            Unknow
        }
        struct Command
        {
            public CommandType Prefix;
            public string[] Content;

            public static CommandType GetCommandType(string s)
            {
                switch (s)
                {
                    case "start":
                        return CommandType.Start;
                    case "add":
                        return CommandType.Add;
                    case "ban":
                        return CommandType.Ban;
                    case "bind":
                        return CommandType.Bind;
                    case "status":
                        return CommandType.Status;
                    case "help": 
                        return CommandType.Help;
                    case "info":
                        return CommandType.Info;
                    case "promote":
                        return CommandType.Promote;
                    case "demote":
                        return CommandType.Demote;
                    case "mai":
                        return CommandType.Mai;
                    case "config":
                        return CommandType.Config;
                    case "logs":
                        return CommandType.Logs;
                    case "set":
                        return CommandType.Set;
                    default:
                        return CommandType.Unknow;
                }
            }
        }
        static void CommandPreHandle(string[] param, Update update)
        {          
            
            var message = update.Message;
            var chat = message.Chat;
            var userId = message.From.Id;
            var user = Config.SearchUser(userId);
            Group group = null;
            var isGroup = chat.Type is (ChatType.Group or ChatType.Supergroup);


            
            if (param.Length == 0  || string.IsNullOrEmpty(param[0]))
                return;
            if (param[0].Substring(0, 1) != "/")
            {
                Debug(DebugType.Info, $"\"{param[0]}\" is not valid command,skipped");
                return;
            }
            if (user is null)
                return;
            if (isGroup)
                group = Config.SearchGroup(chat.Id);

            var _param = param[0].Split("@");
            var prefix = _param[0].Replace("/","");
            if (group is not null && group.Setting.ForceCheckReference)
                if (!(_param.Length == 2 && _param[1] == BotUsername))
                    return;

            if(!user.CheckPermission(Permission.Root))
                if (!user.CheckPermission(Permission.Common,group))
                {
                    SendMessage("很抱歉，您不能使用该Bot哦~", update);
                    Debug(DebugType.Info, "Banned user access,rejected");
                    return;
                }

            Command command = new();

            command.Prefix = Command.GetCommandType(prefix);
            command.Content = param.Skip(1).ToArray();
            CommandHandle(command, update,user,group);
            
        }
        static void CommandHandle(Command command,Update update,TUser querier,Group group)
        {
            var message = update.Message;
            var isPrivate = update.Message.Chat.Type is ChatType.Private;

            switch (command.Prefix)
            {
                case CommandType.Start:
                    SendMessage("你好，我是钟致远，我出生于广西壮族自治区南宁市，从小就擅长滥用，有关我的滥用事迹请移步 @hax_server\n" +
                        "\n请输入 /help 以获得更多信息", update,true);
                    break;
                case CommandType.Add:
                    AddUser(command, update, querier, group);
                    break;
                case CommandType.Ban:
                    BanUser(command, update, querier, group);
                    break;
                case CommandType.Status:
                    GetSystemInfo(command, update);
                    break;
                case CommandType.Info:
                    GetUserInfo(command, update, querier, group);
                    break;
                case CommandType.Promote:
                    SetUserPermission(command, update, querier, 1, group);
                    break;
                case CommandType.Demote:
                    SetUserPermission(command, update, querier, -1, group);
                    break;
                case CommandType.Config:
                    BotConfig(command, update, querier, group);
                    break;
                case CommandType.Help:
                    GetHelpInfo(command, update, querier, group);
                    break;
                case CommandType.Logs:
                    GetBotLog(command, update, querier, group);
                    break;
                case CommandType.Set:
                    AdvancedCommandHandle(command, update, querier, group);
                    break;
                case CommandType.Mai:
                    MaiCommandHandle(command, update, querier, group);
                    break;
            }
        }
        static void FilterGroupMessage(string content, Update update, TUser querier)
        {
            var chat = update.Message?.Chat ?? update.EditedMessage?.Chat;
            if (chat is null)
                return;
            else if (chat.Type is not (ChatType.Group or ChatType.Supergroup))
                return;

            var group = Config.SearchGroup(chat.Id);
            if (group.Rule.Count == 0)
                return;

            var ruleList = group.Rule;
            var commonRuleList = ruleList.Where(x => x.Target is null);
            var specificRuleList = ruleList.Where(x => x.Target.Id == querier.Id);
            var activeRuleList = new List<Filter>();
            activeRuleList.AddRange(specificRuleList);
            activeRuleList.AddRange(commonRuleList);
            Action<Filter> FilterHandler = rule =>
            {
                switch (rule.ActionType)
                {
                    case ActionType.Modify:
                        break;
                    case ActionType.Delete:
                        DeleteMessage(update);
                        break;
                }
                if (rule.ActionString is not null)
                    SendMessage(rule.ActionString, update);
            };

            foreach (var rule in activeRuleList)
            {
                if(update.Message.Type  == rule.MessageType)
                {
                    if(update.Message.Text is null)
                        FilterHandler(rule);
                    else if(rule.MatchString is not null && update.Message.Text.Contains(rule.MatchString))
                        FilterHandler(rule);
                }
            }
            
        }
        static async Task<Message> SendMessage(string text,Update update,bool isReply = true, ParseMode? parseMode = null)
        {
            try
            {
                return await botClient.SendTextMessageAsync(
                        chatId: update.Message.Chat.Id,
                        text: text,
                        replyToMessageId: isReply ? update.Message.MessageId : null,
                        parseMode: parseMode);
            }
            catch(Exception e)
            {
                Debug(DebugType.Error, $"Failure to send message : \n{e.Message}\n{e.StackTrace}");
                return null;
            }
        }
        static async void DeleteMessage(Update update)
        {
            try
            {
                await botClient.DeleteMessageAsync(
                chatId: update.Message.Chat.Id,
                messageId: update.Message.MessageId
                );
            }
            catch (Exception e)
            {
                SendMessage("请为Bot授予删除消息权限喵", update,false);
                Debug(DebugType.Error, $"Cannot delete message : \n{e.Message}\n{e.StackTrace}");
            }
        }
        static async Task<bool> UploadFile(string filePath,long chatId)
        {
            try
            {
                var stream = System.IO.File.Open(filePath,FileMode.Open,FileAccess.Read,FileShare.ReadWrite);
                var filename = new FileInfo(filePath).Name;
                await botClient.SendDocumentAsync(
                        chatId: chatId,
                        document: InputFile.FromStream(stream: stream, fileName: filename));
                stream.Close();
                return true;
            }
            catch(Exception e)
            {
                Debug(DebugType.Error, $"Failure to upload file : \n{e.Message}\n{e.StackTrace}");
                return false;
            }
        }
        static async Task<bool> DownloadFile(string dPath,string fileId)
        {
            try
            {
                await using Stream fileStream = System.IO.File.Create(dPath);
                var file = await botClient.GetInfoAndDownloadFileAsync(
                    fileId: fileId,
                    destination: fileStream);
                return true;
            }
            catch(Exception e)
            {
                Debug(DebugType.Error, $"Failure to download file : \n{e.Message}\n{e.StackTrace}");
                return false;                
            }
        }
        static async Task<Message> EditMessage(string text,Update update, int messageId,ParseMode? parseMode = null)
        {
            try
            {
                return await botClient.EditMessageTextAsync(
                    chatId: update.Message.Chat.Id,
                    messageId: messageId,
                    text: text, parseMode: parseMode);
            }
            catch(Exception e)
            {
                Debug(DebugType.Error, $"Failure to edit message : \n{e.Message}\n{e.StackTrace}");
                return null;
            }
        }
    }
}
