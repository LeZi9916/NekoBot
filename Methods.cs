using System;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace TelegramBot
{
    internal partial class Program
    {
        static void AddUser(Command command, Update update, TUser querier)
        {
            var replyMessage = update.Message.ReplyToMessage;
            TUser target = null; 
            
            if (command.Content.Length == 0)
            {
                if (replyMessage is null)
                {
                    SendMessage("请指明需要修改的用户喵", update);
                    return;
                }
                target = Config.SearchUser(replyMessage.From.Id);
            }
            else
            {
                long id = -1;

                if (long.TryParse(command.Content[0], out id))
                    target = Config.SearchUser(id);
                else
                {
                    SendMessage("userId错误，请仔细检查喵~", update);
                    return;
                }
            }

            if(target is null)
            {
                SendMessage($"喵不认识这个人xAx", update);
                return;
            }
            else if (target.Id == querier.Id)
            {
                SendMessage($"喵喵喵？QAQ", update);
                return;
            }
            else if(target.Level >= Permission.Common)
            {
                SendMessage($"这位朋友咱已经认识啦~", update);
                return;
            }
            else
            {
                target.SetPermission(Permission.Common);
                SendMessage($"欢迎新朋友~", update);
            }
        }
        static void BanUser(Command command, Update update, TUser querier)
        {
            var replyMessage = update.Message.ReplyToMessage;
            TUser target = null;

            if (command.Content.Length == 0)
            {
                if (replyMessage is null)
                {
                    SendMessage("请指明需要修改的用户喵", update);
                    return;
                }
                target = Config.SearchUser(replyMessage.From.Id);
            }
            else
            {
                long id = -1;

                if (long.TryParse(command.Content[0], out id))
                    target = Config.SearchUser(id);
                else
                {
                    SendMessage("userId错误，请仔细检查喵~", update);
                    return;
                }
            }

            if (target is null)
            {
                SendMessage($"喵不认识这个人xAx", update);
                return;
            }
            else if (target.Id == querier.Id)
            {
                SendMessage($"喵喵喵？QAQ", update);
                return;
            }
            else if (target.Level >= querier.Level)
            {
                SendMessage($"你想篡权喵?", update);
                return;
            }
            else
            {
                target.SetPermission(Permission.Ban);
                SendMessage($"已经将坏蛋踢出去了喵", update);
            }
        }
        static void GetUserInfo(Command command, Update update,TUser querier)
        {
            var isGroup = update.Message.Chat.Type is ChatType.Group;
            var id = (update.Message.ReplyToMessage is not null ? (update.Message.ReplyToMessage.From ?? update.Message.From) : update.Message.From).Id;

            if (command.Content.Length > 0)
            {
                if (!long.TryParse(command.Content[0], out id))
                {
                    SendMessage("userId错误，请仔细检查喵~", update);
                    return;
                }
            }
            if (id != querier.Id && !querier.CheckPermission(Permission.Admin))
            {
                SendMessage("很抱歉，您不能查看别人的信息哦~", update);
                return;
            }
            

            var target = Config.SearchUser(id);            
            if (target is null)
                SendMessage("查无此人喵", update);
            else
                SendMessage(
                    $"用户信息:\n" +
                    $"Name: {target.Name}\n" +
                    $"Id: {target.Id}\n" +
                    $"Permission: {target.Level}\n" +
                    $"MaiUserId: {(isGroup ? target.MaiUserId is null ? "未绑定" : "喵" : target.MaiUserId is null ? "未绑定" : target.MaiUserId)}", update);
        }
        static void SetUserPermission(Command command, Update update, TUser querier,int diff)
        {
            var replyMessage = update.Message.ReplyToMessage;
            Func<Permission,TUser, bool> canPromote = (s,user) => 
            {
                switch(s)
                {
                    case Permission.Unknow:
                    case Permission.Ban:
                    case Permission.Common:
                    case Permission.Advanced:
                        return user.CheckPermission(Permission.Admin);
                    case Permission.Admin:
                    case Permission.Root:
                        return user.CheckPermission(Permission.Root);
                    default:
                        return false;
                }
            };
            Func<string, Permission> getLevel = s =>
            {
                return s.ToLower() switch
                {
                    "ban" => Permission.Ban,
                    "common" => Permission.Common,
                    "admin" => Permission.Admin,
                    "root" => Permission.Root,
                    _ => Permission.Unknow
                };
            };
            Action<long> setPermission = id =>
            {
                if (id == querier.Id)
                {
                    SendMessage("不可以修改自己的权限喵",update);
                    return;
                }

                var target = Config.SearchUser(id);
                var targetLevel = target.Level + diff;

                if(target is null)
                {
                    SendMessage($"喵不认识这个人xAx", update);
                    return;
                }    
                if (canPromote(targetLevel, querier))
                {
                    if(targetLevel < Permission.Ban)
                    {
                        SendMessage($"权限已经不能再降低了QAQ", update);
                        return;
                    }
                    else if (targetLevel > Permission.Admin)
                    {
                        SendMessage($"权限已经不能再提高了QAQ", update);
                        return;
                    }

                    if (target.Level >= querier.Level)
                    {
                        SendMessage($"你不可以修改 *{target.Name}*的权限喵xAx", update, parseMode: ParseMode.MarkdownV2);
                        return;
                    }
                    target.SetPermission(targetLevel);
                    SendMessage($"成功将*{target.Name}*\\({target.Id}\\)的权限修改为*{targetLevel}*", update, parseMode:ParseMode.MarkdownV2);
                }
                else
                {
                    SendMessage("很抱歉，您的权限不足喵", update);
                    return;
                }
            };

            if(command.Content.Length == 0)
            {
                if (replyMessage is null)
                {
                    SendMessage("请指明需要修改的用户喵", update);
                    return;
                }
                setPermission(replyMessage.From.Id);                
            }
            else if (command.Content.Length == 1)
            {
                long id = -1;

                if(long.TryParse(command.Content[0],out id))
                    setPermission(id);
                else
                    SendMessage("userId错误，请仔细检查喵~", update);
            }
            else
                SendMessage("喵?", update);
        }        
        static async void GetSystemInfo(Command command, Update update)
        {
            var uptime = DateTime.Now - startTime;
            await SendMessage(StringHandle(
                $"当前版本: v{Assembly.GetExecutingAssembly().GetName().Version}\n\n" +
                "系统信息:\n" +
                $"-核心数: {Monitor.ProcessorCount}\n" +
                $"-使用率: {Monitor.CPULoad}%\n" +
                $"-总内存: {Monitor.TotalMemory/1000000} MiB\n" +
                $"-剩余内存: {Monitor.FreeMemory/1000000} MiB\n" +
                $"-已用内存: {Monitor.UsedMemory/1000000} MiB ({Monitor.UsedMemory * 100 / Monitor.TotalMemory }%)\n" +
                $"-在线时间: {uptime.Hours}h{uptime.Minutes}m{uptime.Seconds}s\n\n" +
                $"-总计处理消息数: {Config.TotalHandleCount}\n" +
                $"-平均耗时: {(Config.TotalHandleCount is 0 ? 0 :Config.TimeSpentList.Sum() / Config.TotalHandleCount)}ms\n" +
                $"-5分钟平均CPU占用率: {Monitor._5CPULoad}%\n" +
                $"-10分钟平均CPU占用率: {Monitor._10CPULoad}%\n" +
                $"-15分钟平均CPU占用率: {Monitor._15CPULoad}%\n"),update,true,ParseMode.MarkdownV2);
        }
        static string GetRandomStr() => Convert.ToBase64String(SHA512.HashData(Guid.NewGuid().ToByteArray()));
        
    }
    
}
