using CSScripting;
using System;
using System.Data;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using TelegramBot.Class;

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
        
        static void CommandPreHandle(string[] param, Update update)
        {
            if (param.Length == 0 || string.IsNullOrEmpty(param[0]))
                return;

            var message = update.Message;
            var chat = message.Chat;
            var userId = message.From.Id;
            var user = Config.SearchUser(userId);
            Group group = null;
            var isGroup = chat.Type is (ChatType.Group or ChatType.Supergroup);

            //分割 /status@botusername
            var _param = param[0].Split("@");
            var prefix = _param[0].Replace("/", "");

            
            if (param[0].Substring(0, 1) != "/" || BotCommands.Where(x => x.Command == prefix).IsEmpty())
            {
                Debug(DebugType.Debug, $"\"{param[0]}\" is not valid command,skipped");
                return;
            }
            if (user is null)
                return;
            else if (isGroup)
                group = Config.SearchGroup(chat.Id);

            param = param.Skip(1).ToArray();


            if (group is not null && group.Setting.ForceCheckReference)
                if (!(_param.Length == 2 && _param[1] == BotUsername))
                    return;
            
            if (!user.isNormal)
            {
                var sArray = string.Join(" ", param).Split("-token");
                if (sArray.Length < 2)
                {
                    SendMessage("Authentication failed:\nGroup or channel anonymous access is not allowed", update);
                    Debug(DebugType.Info, "Channel access,rejected");
                    return;
                }

                //var sArray = param[1].Split("--token");
                var token = sArray[1];
                if (!Config.Authenticator.Compare(token.Trim()))
                {
                    SendMessage("Authentication failed:\nInvalid HOTP code", update);
                    Debug(DebugType.Info, "HOTP code is invalid,rejected");
                    return;
                }
                else
                    param = sArray[0].Split(" ",StringSplitOptions.RemoveEmptyEntries);

            }

            if (!user.CheckPermission(Permission.Root))
                if (!user.CheckPermission(Permission.Common,group))
                {
                    SendMessage("Access Denied", update);
                    Debug(DebugType.Info, "Banned user access,rejected");
                    return;
                }

            InputCommand command = new();

            command.Prefix = prefix;
            command.Content = param;
            ScriptManager.CommandHandle(command, update,user,group);
            
        }
        public static async Task<Message> SendMessage(string text,Update update,bool isReply = true, ParseMode? parseMode = null)
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
                SendMessage("请为Bot授予删除消息权限喵", update,false);
                Debug(DebugType.Error, $"Cannot delete message : \n{e.Message}\n{e.StackTrace}");
            }
        }
        public static async Task<bool> UploadFile(string filePath,long chatId)
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
        public static async Task<bool> UploadFile(Stream stream,string fileName, long chatId)
        {
            try
            {
                await botClient.SendDocumentAsync(
                        chatId: chatId,
                        document: InputFile.FromStream(stream: stream,fileName : fileName));
                stream.Close();
                return true;
            }
            catch (Exception e)
            {
                Debug(DebugType.Error, $"Failure to upload file : \n{e.Message}\n{e.StackTrace}");
                return false;
            }
        }
        public static async Task<bool> DownloadFile(string dPath,string fileId)
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
        public static async Task<Message> EditMessage(string text,Update update, int messageId,ParseMode? parseMode = null)
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
