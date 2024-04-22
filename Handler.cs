using CSScripting;
using System;
using System.Data;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using TelegramBot.Types;
using Message = TelegramBot.Types.Message;

namespace TelegramBot
{
    public partial class Program
    {
        public static Func<string, string> StringHandle = s =>
        {
            string[] reservedChar = { "_", "*", "[", "]", "(", ")", "~", "`", ">", "#", "+", "-", "=", "|", "{", "}", ".", "!" };
            foreach (var c in reservedChar)
                s = s.Replace(c, $"\\{c}");
            return s;

        };

        static void CommandPreHandle(Message msg)
        {
            if (msg.Command is null)
                return;

            var group = msg.IsGroup ? Config.SearchGroup(msg.Chat.Id) : null;
            var user = msg.From;
            var cmd = (Command)msg.Command;

            // Reference check
            if (cmd.Prefix.Contains("@"))
            {
                var prefix = cmd.Prefix;
                var s = prefix.Split("@", 2, StringSplitOptions.RemoveEmptyEntries);

                if ((s.Length != 2 || s[1] != BotUsername) && 
                    (group is not null && group.Setting.ForceCheckReference))
                    return;

                cmd.Prefix = s.First();
                msg.Command = cmd;
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
                $"From: {msg.From.Name}({msg.From.Id})\n" +
                $"Chat: {msg.Chat.Id}\n" +
                $"Permission: {msg.From.Level.ToString()}\n" +
                $"Prefix: /{cmd.Prefix}\n" +
                $"Params: {string.Join(" ", cmd.Params)}");
            ScriptManager.CommandHandle(msg);

            //if (param.Length == 0 || string.IsNullOrEmpty(param[0]))
            //    return;

            //var message = update.Message;
            //var chat = message.Chat;
            //var userId = message.From.Id;
            //var user = Config.SearchUser(userId);
            //Group group = null;
            //var isGroup = chat.Type is (ChatType.Group or ChatType.Supergroup);

            ////分割 /status@botusername
            //var _param = param[0].Split("@");
            //var prefix = _param[0].Replace("/", "");


            //if (param[0].Substring(0, 1) != "/" || BotCommands.Where(x => x.Command == prefix).IsEmpty())
            //{
            //    Debug(DebugType.Debug, $"\"{param[0]}\" is not valid command,skipped");
            //    return;
            //}
            //if (user is null)
            //    return;
            //else if (isGroup)
            //    group = Config.SearchGroup(chat.Id);

            //param = param.Skip(1).ToArray();


            //if (group is not null && group.Setting.ForceCheckReference)
            //    if (!(_param.Length == 2 && _param[1] == BotUsername))
            //        return;

            //if (!user.IsNormal)
            //{
            //    var sArray = string.Join(" ", param).Split("-token");
            //    if (sArray.Length < 2)
            //    {
            //        SendMessage("Authentication failed:\nGroup or channel anonymous access is not allowed", update);
            //        Debug(DebugType.Info, "Channel access,rejected");
            //        return;
            //    }

            //    //var sArray = param[1].Split("--token");
            //    var token = sArray[1];
            //    if (!Config.Authenticator.Compare(token.Trim()))
            //    {
            //        SendMessage("Authentication failed:\nInvalid HOTP code", update);
            //        Debug(DebugType.Info, "HOTP code is invalid,rejected");
            //        return;
            //    }
            //    else
            //        param = sArray[0].Split(" ",StringSplitOptions.RemoveEmptyEntries);

            //}

            //if (!user.CheckPermission(Permission.Root))
            //    if (!user.CheckPermission(Permission.Common,group))
            //    {
            //        SendMessage("Access Denied", update);
            //        Debug(DebugType.Info, "Banned user access,rejected");
            //        return;
            //    }

            //Command command = new();

            //command.Prefix = prefix;
            //command.Params = param;
            //ScriptManager.CommandHandle(command, update,user,group);

        }
        public static async Task DeleteMessage(Update update)
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
                Debug(DebugType.Error, $"Cannot delete message : \n{e.Message}\n{e.StackTrace}");
            }
        }
        public static async Task<bool> UploadFile(string filePath, long chatId)
        {
            try
            {
                var stream = System.IO.File.Open(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
                var filename = new FileInfo(filePath).Name;
                await botClient.SendDocumentAsync(
                        chatId: chatId,
                        document: InputFile.FromStream(stream: stream, fileName: filename));
                stream.Close();
                return true;
            }
            catch (Exception e)
            {
                Debug(DebugType.Error, $"Failure to upload file : \n{e.Message}\n{e.StackTrace}");
                return false;
            }
        }
        public static async Task<bool> UploadFile(Stream stream, string fileName, long chatId)
        {
            try
            {
                await botClient.SendDocumentAsync(
                        chatId: chatId,
                        document: InputFile.FromStream(stream: stream, fileName: fileName));
                stream.Close();
                return true;
            }
            catch (Exception e)
            {
                Debug(DebugType.Error, $"Failure to upload file : \n{e.Message}\n{e.StackTrace}");
                return false;
            }
        }
    }
}
